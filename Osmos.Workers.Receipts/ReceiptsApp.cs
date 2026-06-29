using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Extensions;
using Osmos.Workers.Helpers.Eth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Osmos.Workers.Receipts
{
    internal class ReceiptsApp : BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { GetReceiptsAsync, SetReceiptAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private readonly object _receiptsLock = new object();
        private List<TransactionReceipt> _pendingReceipts = new List<TransactionReceipt>();
        private BigInteger _processedBlock = new BigInteger(0);

        private async Task GetReceiptsLoop() {
            var lastBlock = await _ethService.GetLastBlockAsync();

            //lastBlock = new HexBigInteger(lastBlock.Value - 1);

            if (lastBlock != _processedBlock)
            {
                Console.WriteLine($"new block: {lastBlock.Value} mined!");

                //var txnList = await MemepoolService.GetRawTransactionsAtMinedBlockAsync(lastBlock);

                ReceiptsResult result = null;

                //int i = 0;
                while (result == null)
                {
                    result = await _ethService.GetReceiptsAsync(lastBlock);

                    //Console.WriteLine($"iteration {i + 1}");
                    if (result == null) await Task.Delay(222);
                }

                var receipts = result.receipts.ToArray();
                Console.WriteLine($"found {receipts.Length} new receipts at block {lastBlock}");

                if (receipts.Any())
                {
                    lock (_receiptsLock)
                    {
                        _pendingReceipts.AddRange(receipts);
                        Console.WriteLine($"pendingReceipts length: {_pendingReceipts.Count}");
                    }
                }

                _processedBlock = lastBlock;
            }
            else
            {
                await Task.Delay(1212);
                //Console.WriteLine("same block");
            }
        }

        private async Task GetReceiptsAsync()
        {
            await ActionsHelper.LoopAsync(GetReceiptsLoop, 222);
        }

        private async Task SetReceiptLoop() {
            var now = DateTime.UtcNow;

            TransactionReceipt[] receipts = null;
            lock (_receiptsLock)
            {
                if (!_pendingReceipts.Any()) return;

                receipts = _pendingReceipts.ToArray();


                _pendingReceipts = new List<TransactionReceipt>();
            }

            var blockNumber = receipts.First().BlockNumber;

            Console.WriteLine($"got {receipts.Length} receipts from memory at block {blockNumber}");

            var blockTransactions = await _ethService.GetBlockTransactionsAsync(blockNumber); ;

            var hashes = receipts.Select(t => t.TransactionHash).ToArray();

            //await txnsManager.DeleteManyAsync(t => hashes.Contains(t.Hash) && t.Receipt == null);
            var existingTxns = await _txnsManager.GetManyAsync(t => hashes.Contains(t.Hash));

            var toCreate = new List<Txn>();
            foreach (var receipt in receipts)
            {
                ActionsHelper.SmoothRun(() => {
                    var txnReceipt = new TxnReceipt
                    {
                        Succeeded = receipt.Status == new HexBigInteger(1),
                        CumulativeGasUsed = receipt.CumulativeGasUsed.ToString(),
                        EffectiveGasPrice = receipt.EffectiveGasPrice.ToString(),
                        GasUsed = receipt.GasUsed.ToString(),
                        Type = receipt.Type.ToString(),
                        BlockHash = receipt.BlockHash,
                        BlockNumber = receipt.BlockNumber.ToString(),
                        Logs = receipt.Logs?.Select(l => l.ToString()).ToArray(),
                        LogsBloom = receipt.LogsBloom,
                        TransactionHash = receipt.TransactionHash,
                        TransactionIndex = receipt.TransactionIndex.ToString(),
                        Status = receipt.Status?.ToString(),
                        ContractAddress = receipt.ContractAddress,
                        From = receipt.From,
                        Root = receipt.Root,
                        To = receipt.To
                    };

                    var blockTransaction = blockTransactions.FirstOrDefault(t => t.TransactionHash == txnReceipt.TransactionHash);

                    var newTransaction = blockTransaction.ToTxn(txnReceipt);

                    if (newTransaction.IsApprove)
                    {
                        var existingTxn = existingTxns.FirstOrDefault(t => t.Hash == newTransaction.Hash);
                        if (existingTxn != null)
                        {
                            newTransaction.GotTokenInfo = existingTxn.GotTokenInfo;
                        }
                    }

                    toCreate.Add(newTransaction);
                });
            }

            foreach (var txn in toCreate)
            {
                ActionsHelper.SmoothRun(() => {
                    if (FunctionSignaturesHelper.IsNotFound(txn.FunctionSignature)) return;

                    string functionName = FunctionSignaturesHelper.GetFunctionName(txn.FunctionSignature);
                    if (functionName == null) return;

                    txn.FunctionName = functionName;
                });
            }

            await _txnsManager.DeleteManyAsync(t => hashes.Contains(t.Hash));

            if (toCreate.Any())
            {
                await _txnsManager.CreateManyAsync(toCreate.ToArray());
            }

            Console.WriteLine($"saved {toCreate.Count} txns in {(DateTime.UtcNow - now).TotalSeconds} seconds");
        }

        private async Task SetReceiptAsync()
        {
            await ActionsHelper.LoopAsync(SetReceiptLoop, 222);
        }

        #region internals

        private readonly TxnsManager _txnsManager = null;
        private readonly EthService _ethService = null;

        public ReceiptsApp(IOptions<FunctionSignatureSettings> functionSignatureSettings, TxnsManager txnsManager, EthService ethService)
        {
            _txnsManager = txnsManager;

            FunctionSignaturesHelper.Init(functionSignatureSettings.Value.FolderName);

            _ethService = ethService;

        }

        #endregion
    }
}
