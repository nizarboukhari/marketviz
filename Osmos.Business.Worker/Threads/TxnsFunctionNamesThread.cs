using Nethereum.Contracts;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Services;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Threads
{
    public static class TxnsFunctionNamesThread
    {
        public static void Start(TxnsManager txnsManager) {
            var loop = new Thread(new ThreadStart(() => GetTxnFunctionNamesLoop(txnsManager)));
            loop.Start();
        }

        private static Logger _logger = new Logger("TxnsFunctionNamesThread", true);

        private static async Task<Txn[]> GetTxnFunctionNames(Txn[] txns)
        {
            int fromFile = 0;
            int fromContract = 0;
            int fromApi = 0;
            int notFound = 0;

            foreach (var txn in txns)
            {
                var now = DateTime.UtcNow;

                // remark: many txns have an input of "0x" or "0x00" ==  transfert
                string functionSignature = txn.Input.ToFunctionName();
                string functionName = null;

                try
                {
                    if (functionSignature != null)
                    {
                        if (FunctionSignaturesHelper.IsNotFound(functionSignature))
                        {

                            txn.FunctionSignature = functionSignature;
                            txn.FunctionName = $"0x{functionSignature}";

                            notFound++;
                            continue;
                        }

                        functionName = FunctionSignaturesHelper.GetFunctionName(functionSignature);

                        if (functionName != null) fromFile++;

                        if (functionName == null)
                        {
                            Contract contract = null;
                            try
                            {
                                contract = await TokenService.GetContractAsync(txn.To);
                            }
                            catch (Exception ce)
                            {
                                ;
                            }

                            if (contract != null) {
                                var signs = contract.ContractBuilder.ContractABI.Functions
                                   .Select(_ => _.Sha3Signature)
                                   .ToArray();

                                var keyValues = contract.ContractBuilder.ContractABI.Functions
                                    .Select(_ => new KeyValuePair<string, string>(_.Sha3Signature, _.Name))
                                    .ToDictionary(_ => _.Key, _ => _.Value);

                                FunctionSignaturesHelper.WriteManyInTable(keyValues);

                                functionName = FunctionSignaturesHelper.GetFunctionName(functionSignature);

                                if (functionName != null) fromContract++;
                            }
                        }

                        if (functionName == null)
                        {
                            functionName = await FunctionSignatureApi.GetAsync(functionSignature);

                            if (functionName != null)
                            {
                                FunctionSignaturesHelper.WriteOneInTable(functionSignature, functionName);
                                fromApi++;
                            }
                            else
                            {
                                functionName = $"0x{functionSignature}";
                                FunctionSignaturesHelper.WriteOneInNotFound(functionSignature);
                                notFound++;
                            }
                        }
                    }
                    else
                    {
                        // can not get function signature
                        ;
                    }

                    txn.FunctionSignature = functionSignature;
                    txn.FunctionName = functionName;

                }
                catch (Exception ex)
                {
                    txn.FunctionSignature = functionSignature;
                    txn.FunctionName = $"0x{functionSignature}";
                }
            }


            return txns;
        }

        private static async void GetTxnFunctionNamesLoop(TxnsManager txnsManager)
        {
            do
            {
                Thread.Sleep(500);

                try
                {
                    var options = new QueryOptions<Txn>
                    {
                        SortOptions = new QuerySortOptions<Txn>
                        {
                            Field = t => t.CreatedDate,
                            Descending = true
                        },
                        Pagination = new QueryPaginationOptions
                        {
                            Size = 100
                        }
                    };

                    var response = await txnsManager.GetManyAsync(options, _ => _.FunctionName == null);

                    _logger.Write($"remaining txns {response.Total}");
                    var txns = response.Entities;

                    if (!txns.Any()) continue;

                    txns = await GetTxnFunctionNames(txns);

                    var now = DateTime.UtcNow;

                    var toUpdate = txns.Where(txn => txn.FunctionName != null);
                    //await txnsManager.BlukUpdateNamesAsync(txns);
                    await Task.WhenAll(toUpdate.Select(txn => Task.Run(async () => {
                        await txnsManager.UpdateOneAsync(txn);
                    })));

                    _logger.Write($"updated {toUpdate.Count()} txn names in {(DateTime.UtcNow - now).TotalSeconds}");
                }
                catch (Exception ex)
                {

                }

            } while (true);

        }
    }
}
