using MongoDB.Driver;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Models;
using Osmos.Business.Worker.Services;
using Osmos.Core.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Threads
{
    public static class ReceiptsThread
    {
        public static void Start(TxnsManager txnsManager)
        {
            var setLoop = new Thread(new ThreadStart(() => GetReceiptsLoop()));
            var getLoop = new Thread(new ThreadStart(() => SetReceiptLoop(txnsManager)));


            setLoop.Start();
            getLoop.Start();
        }

        private static readonly object _receiptsLock = new object();
        private static List<TransactionReceipt> _pendingReceipts = new List<TransactionReceipt>();

        private static Logger _logger = new Logger("ReceiptsThread", false);

        private static async Task<ReceiptsResult> RequestReceiptsAsync(HexBigInteger lastBlock)
        {
            string body = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"alchemy_getTransactionReceipts\",\"params\":[{\"blockNumber\":\"" + lastBlock.HexValue.ToString() + "\"}]}";

            var options = new ApiRequestOptions
            {
                Uri = Settings.AlchemyAPIURL,
                Method = HttpMethod.Post,
                Content = body
            };
            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return null;

            var json = JsonConvert.DeserializeObject<Receipts>(response.ApiResponse.StringContent);
            return json.result;
        }

        private static async void GetReceiptsLoop()
        {
            var processedBlock = new BigInteger(0);
            do
            {
                try
                {
                    var web3Ws = new Web3(Settings.EthWsURL);
                    var lastBlock = await web3Ws.Eth.Blocks.GetBlockNumber.SendRequestAsync();

                    //lastBlock = new HexBigInteger(lastBlock.Value - 1);

                    if (lastBlock != processedBlock)
                    {
                        _logger.Write($"new block: {lastBlock.Value} mined!");

                        //var txnList = await MemepoolService.GetRawTransactionsAtMinedBlockAsync(lastBlock);

                        ReceiptsResult result = null;

                        while (result == null)
                        {
                            result = await RequestReceiptsAsync(lastBlock);

                            if (result == null) Thread.Sleep(100);
                        }

                        var receipts = result.receipts.ToArray();
                        _logger.Write($"found {receipts.Length} new receipts at block {lastBlock}");

                        if (receipts.Any())
                        {
                            lock (_receiptsLock)
                            {
                                _pendingReceipts.AddRange(receipts);
                                _logger.Write($"pendingReceipts length: {_pendingReceipts.Count}");
                            }
                        }

                        processedBlock = lastBlock;
                    }
                    else
                        Thread.Sleep(250);

                }
                catch (Exception e)
                {
                    ;
                }
            } while (true);
        }

        private static async void SetReceiptLoop(TxnsManager txnsManager)
        {
            do
            {
                try
                {
                    TransactionReceipt[] receipts = null;
                    lock (_receiptsLock)
                    {
                        if (!_pendingReceipts.Any()) continue;

                        receipts = _pendingReceipts.ToArray();


                        _pendingReceipts = new List<TransactionReceipt>();
                    }

                    var blockNumber = receipts.First().BlockNumber;

                    _logger.Write($"got {receipts.Length} receipts from MEMORY at block {blockNumber}");

                    var blockTransactions = await MemepoolService.GetTransactionsAtBlock(blockNumber);

                    var hashes = receipts.Select(t => t.TransactionHash).ToArray();

                    //await txnsManager.DeleteManyAsync(t => hashes.Contains(t.Hash) && t.Receipt == null);
                    var existingTxns = await txnsManager.GetManyAsync(t => hashes.Contains(t.Hash));
                    
                    var toCreate = new List<Txn>();
                    foreach (var receipt in receipts)
                    {
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
                            Status = receipt.Status.ToString(),
                            ContractAddress = receipt.ContractAddress,
                            From = receipt.From,
                            Root = receipt.Root,
                            To = receipt.To
                        };

                        var blockTransaction = blockTransactions.FirstOrDefault(t => t.TransactionHash == txnReceipt.TransactionHash);

                        var newTransaction = blockTransaction.ToTxn(txnReceipt);

                        if (newTransaction.IsApprove) {
                            var existingTxn = existingTxns.FirstOrDefault(t => t.Hash == newTransaction.Hash);
                            if (existingTxn != null)
                            {
                                newTransaction.GotTokenInfo = existingTxn.GotTokenInfo;
                            }
                        }

                        toCreate.Add(newTransaction);
                    }

                    foreach (var txn in toCreate)
                    {
                        if (FunctionSignaturesHelper.IsNotFound(txn.FunctionSignature)) continue;

                        string functionName = FunctionSignaturesHelper.GetFunctionName(txn.FunctionSignature);
                        if (functionName == null) continue;

                        txn.FunctionName = functionName;
                    }

                    await txnsManager.DeleteManyAsync(t => hashes.Contains(t.Hash));

                    if (toCreate.Any()) {
                        await txnsManager.CreateManyAsync(toCreate.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    ;
                }
            } while (true);
        }
    }
}
