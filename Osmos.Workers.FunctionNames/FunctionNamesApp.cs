using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Web3;
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.ApiClient;
using Osmos.Core.Data;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Extensions;
using Osmos.Workers.Helpers.Eth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Workers.FunctionNames
{
    internal class FunctionNamesApp: BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { GetTxnFunctionNamesAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private async Task<Txn[]> GetTxnFunctionNames(Txn[] txns)
        {
            int fromFile = 0;
            int fromContract = 0;
            int fromApi = 0;
            int notFound = 0;

            foreach (var txn in txns)
            {
                var now = DateTime.UtcNow;

                // remark: many txns have an input of "0x" or "0x00" ==  transfert
                string functionSignature = txn.Input?.ToFunctionName();
                string functionName = null;

                if (functionSignature == "3593564c") {
                    ;
                }

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
                                contract = await _ethService.GetContractAsync(txn.To);
                            }
                            catch (Exception ce)
                            {
                                ;
                            }

                            if (contract != null)
                            {
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

        private async Task GetTxnFunctionNamesLoop() {
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

            var response = await _txnsManager.GetManyAsync(options, _ => _.FunctionName == null);

            Console.WriteLine($"remaining txns {response.Total}");
            var txns = response.Entities;

            if (!txns.Any()) return;

            txns = await GetTxnFunctionNames(txns);

            var now = DateTime.UtcNow;

            var toUpdate = txns.Where(txn => txn.FunctionName != null);
            await _txnsManager.BlukUpdateNamesAndSignaturesAsync(txns);

            Console.WriteLine($"updated {toUpdate.Count()} txn names in {(DateTime.UtcNow - now).TotalSeconds} seconds");
        }

        private async Task GetTxnFunctionNamesAsync()
        {
            await ActionsHelper.LoopAsync(GetTxnFunctionNamesLoop, 500);
        }

        #region internals

        private readonly TxnsManager _txnsManager = null;
        private readonly EthService _ethService = null;

        public FunctionNamesApp(IOptions<FunctionSignatureSettings> functionSignatureSettings, TxnsManager txnsManager, EthService ethService)
        {
            _txnsManager = txnsManager;

            FunctionSignaturesHelper.Init(functionSignatureSettings.Value.FolderName);

            _ethService = ethService;
        }

        #endregion
    }
}
