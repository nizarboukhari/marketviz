using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Worker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    public class RawTransactionsResult
    {
        public HexBigInteger BlockNumber { get; set; }
        public List<Transaction> Transactions { get; set; }
        public int TotalTransactions { get; set; }
    }


    class MemepoolService
    {
        public static async Task<IEnumerable<Transaction>> GetPendingTransactions()
        {
            var web3 = new Web3(Settings.EthWsURL);

            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());

            return block.Transactions;
        }

        public static async Task<IEnumerable<Transaction>> GetTransactionsAtBlock(HexBigInteger HexBigInteger)
        {
            var web3 = new Web3(Settings.EthWsURL);

            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(HexBigInteger);

            return block.Transactions;
        }

        public static async Task<RawTransactionsResult> GetRawTransactionsAtApprovedBlockAsync(HexBigInteger blockNumber)
        {
            var web3 = new Web3(Settings.EthWsURL);

            //var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumber);

            var notTransferFunctions = block.Transactions.Where(_ => !_.IsTransactionForFunctionMessage<TransferFunction>()).ToList();

            return new RawTransactionsResult
            {
                BlockNumber = block.Number,
                Transactions = notTransferFunctions,
                TotalTransactions = block.Transactions.Length
            };
        }

        public static async Task<RawTransactionsResult> GetRawTransactionsAsync()
        {
            var web3 = new Web3(Settings.EthWsURL);

            //var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            //var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumber);
            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());

            var notTransferFunctions = block.Transactions.Where(_ => !_.IsTransactionForFunctionMessage<TransferFunction>()).ToList();

            return new RawTransactionsResult
            {
                BlockNumber = block.Number,
                Transactions = notTransferFunctions,
                TotalTransactions = block.Transactions.Length
            };
        }

        public static async Task<Txn> GetProcessedTransactionAsync(Transaction txn)
        {
            // TODO : build contract from db instead
            // same check as in approval thread
            var contract = await TokenService.GetContractAsync(txn.To);

            if (txn.Input.Length < 10)
            {
                ;
            }
            var functionSignature = txn.Input.Substring(2, 8);

            var function = contract.ContractBuilder.ContractABI.Functions
                .FirstOrDefault(item => item.Sha3Signature == functionSignature);

            var transaction = new Txn
            {
                Hash = txn.TransactionHash,
                From = txn.From,
                To = txn.To,
                FunctionName = function?.Name,
                FunctionSignature = functionSignature,
                Input = txn.Input.ToString(),
                Value = txn.Value.Value.ToString(),
                Gas = txn.Gas.ToString(),
                Nonce = txn.Nonce.ToString()
            };

            if (txn.MaxFeePerGas != null)
            {
                transaction.MaxFeePerGas = txn.MaxFeePerGas.ToString();
            }
            if (txn.MaxPriorityFeePerGas != null)
            {
                transaction.MaxPriorityFeePerGas = txn.MaxPriorityFeePerGas.ToString();
            }
            if (txn.GasPrice != null)
            {
                transaction.GasPrice = txn.GasPrice.ToString();
            }

            return transaction;
        }

        public static async Task<Transaction> GetTransactionByHashAsync(string hash)
        {
            var web3 = new Web3(Settings.EthWsURL);
            var request = new EthGetTransactionByHash(web3.Client);

            var transaction = await request.SendRequestAsync(hash);
            return transaction;
        }

        [Obsolete]
        private static string _GetFunctionSignature(string txnInput)
        {
            try
            {
                string functionSignature = txnInput.Substring(2, 8);
                return functionSignature;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<Txn[]> GetTxnDetails(FunctionSignaturesTable functionSignaturesTable, Txn[] txns)
        {
            int fromFile = 0;
            int fromContract = 0;
            int fromApi = 0;

            foreach (var txn in txns)
            {
                // remark: many txns have an input of "0x" or "0x00" ==  transfert
                string functionSignature = _GetFunctionSignature(txn.Input);
                string functionName = null;

                try
                {
                    if (functionSignature != null)
                    {
                        functionName = await functionSignaturesTable.GetNameAsync(functionSignature);

                        if (functionName != null) fromFile++;

                        if (functionName == null)
                        {
                            var contract = await TokenService.GetContractAsync(txn.To);

                            var signs = contract.ContractBuilder.ContractABI.Functions
                                .Select(_ => _.Sha3Signature)
                                .ToArray();

                            var keyValues = contract.ContractBuilder.ContractABI.Functions
                                .Select(_ => new KeyValuePair<string, string>(_.Sha3Signature, _.Name))
                                .ToDictionary(_ => _.Key, _ => _.Value);

                            await functionSignaturesTable.WriteManyAsync(keyValues);

                            functionName = await functionSignaturesTable.GetNameAsync(functionSignature);

                            if (functionName != null) fromContract++;
                        }

                        if (functionName == null)
                        {
                            functionName = await FunctionSignatureApi.GetAsync(functionSignature);

                            if (functionName != null) fromApi++;

                            if (functionName != null)
                            {
                                await functionSignaturesTable.WriteOneAsync(functionSignature, functionName);
                            }
                            else
                            {
                                functionName = $"0x{functionSignature}";
                            }
                        }
                    }
                    else
                    {
                        // can not get function signature;
                    }

                    // remarks : this take time, should store the token and check if it already exists??
                    //var contract = await TokenService.GetContractAsync(txn.To);

                    //var function = contract.ContractBuilder.ContractABI.Functions
                    //	.FirstOrDefault(item => item.Sha3Signature == functionSignature);

                    //if (function == null) continue;

                    txn.FunctionSignature = functionSignature;
                    txn.FunctionName = functionName;

                }
                catch (Exception ex)
                {
                    txn.FunctionSignature = functionSignature;
                    txn.FunctionName = $"0x{functionSignature}";
                }
            }

            //Console.WriteLine($"GetTxnDetails: file={fromFile} | contract={fromContract} | api={fromApi}");

            return txns;
        }

        public static async Task<List<Txn>> GetPendingTxns()
        {
            Web3 web3 = new Web3(Settings.EthWsURL);
            BlockWithTransactions block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());
            //Console.WriteLine($"GetPendingTxn: current block =======> {block.Number}");
            //Console.WriteLine("======================================");
            List<Txn> transactions = new List<Txn>();

            foreach (var transaction in block.Transactions)
            {
                try
                {
                    var txn = transaction.ToTxn();

                    transactions.Add(txn);

                }
                catch (Exception ex)
                {

                }

            }

            return transactions;

        }
    }
}
