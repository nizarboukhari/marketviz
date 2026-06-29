using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.ApprovedTokens
{
    internal class ApprovedTokensApp : BaseApp
    {
        protected override async Task RunAsync()
        {
           _ignoredTokenAddresses = await _ignoredTokensManager.GetAllAsync();

            var functions = new Func<Task>[] { ApprovedTokensAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private List<string> _ignoredTokenAddresses = new List<string>();

        private async Task ApprovedTokensLoop()
        {
            var now = DateTime.UtcNow;

            var txns = await _txnsManager.GetManyAsync(t => t.IsApprove && !t.ApprovalDone && t.Receipt != null && t.Receipt.Succeeded);

            var ignoredTxnIds = txns.Where(t => _ignoredTokenAddresses.Contains(t.To.ToLower())).Select(t => t.Id).ToArray();
            await _txnsManager.DeleteManyAsync(t => ignoredTxnIds.Contains(t.Id));

            var tokenAddresses = txns.Select(t => t.To.ToLower()).Distinct().Where(a => !_ignoredTokenAddresses.Contains(a.ToLower())).ToArray();
            var existingTokens = await _tokensManager.GetManyAsync(tk => tokenAddresses.Contains(tk.Address));

            var toCreate = new List<Token>();
            var toUpdate = new List<Token>();

            var doneTxns = txns.Where(t => !_ignoredTokenAddresses.Contains(t.To.ToLower())).ToArray();

            if (!tokenAddresses.Any() || !doneTxns.Any()) return;

            Console.WriteLine($"found {doneTxns.Count()} approve txns for {tokenAddresses.Count()} tokens");

            foreach (var address in tokenAddresses)
            {
                var tokenTxns = doneTxns.Where(t => t.To == address).ToArray();

                var existingToken = existingTokens.FirstOrDefault(tk => tk.Address == address);

                string name = existingToken?.Name;
                string symbol = existingToken?.Symbol;
                string owner = existingToken?.Owner;
                var pairs = existingToken?.Pairs;

                TokenSourceCode sourceCode = existingToken?.SourceCode;
                Pair pairTradingData = existingToken?.PairTradingData;

                if (existingToken == null)
                {
                    if (sourceCode == null || sourceCode.MissingSourceCode() || sourceCode.ABI == _ethService.Settings.DefautltABI)
                    {
                        sourceCode = await _ethService.GetSourceCodeSmoothAsync(address);
                    }

                    string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : _ethService.Settings.DefautltABI;
                    var tokenContract = _ethService.GetContractWithABISmooth(address, abi);

                    var tasks = new List<Task>();

                    if (tokenContract != null)
                    {
                        if (name == null)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                name = await _ethService.GetTokenNameAsync(tokenContract);
                            }));
                        }
                        if (symbol == null)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                symbol = await _ethService.GetTokenSymbolAsync(tokenContract);
                            }));
                        }

                        tasks.Add(Task.Run(async () =>
                        {
                            // renounce ownership logic here
                            owner = await _ethService.GetTokenOwnerAsync(tokenContract);
                        }));
                    }

                    await Task.WhenAll(tasks);


                    if (name == null || symbol == null)
                    {
                        _ignoredTokenAddresses.Add(address);
                        await _ignoredTokensManager.CreateOneAsync(address);
                        continue;
                    }
                }

                //double? price = null;
                //ActionsHelper.SmoothRun(() =>
                //{
                //    if (pairTradingData != null && double.TryParse(pairTradingData.PriceUsd, out double _price))
                //    {
                //        price = _price;
                //    }
                //});

                //var tokenPrice = new TokenPrice
                //{
                //    Value = price
                //};

                if (existingToken == null)
                {
                    existingToken = new Token
                    {
                        Address = address,
                        Name = name,
                        Symbol = symbol,
                        Owner = owner,
                        SourceCode = sourceCode,
                        Pairs = pairs,
                        LastApprovedDate = tokenTxns.OrderBy(_ => _.CreatedDate).Last().CreatedDate,
                        ApprovedDates = tokenTxns.OrderBy(_ => _.CreatedDate).Select(_ => new ApprovalDate
                        {
                            Date = _.CreatedDate,
                            TransactionHash = _.Hash
                        }).ToList(),
                        Approvals = tokenTxns.Length,
                        //Prices = new List<TokenPrice> {
                        //            tokenPrice
                        //        },
                        ApproveTxns = tokenTxns
                    };

                    toCreate.Add(existingToken);
                }
                else
                {
                    existingToken.Name = name;
                    existingToken.Symbol = symbol;
                    existingToken.Owner = owner;
                    existingToken.SourceCode = sourceCode;
                    existingToken.Pairs = pairs;

                    //if (existingToken.Prices.LastOrDefault() == null)
                    //{
                    //    existingToken.Prices = new List<TokenPrice> {
                    //                tokenPrice
                    //            };
                    //}
                    //else if (existingToken.Prices.LastOrDefault().Value != price)
                    //{
                    //    existingToken.Prices.Add(tokenPrice);
                    //}

                    existingToken.ApproveTxns = tokenTxns;
                    toUpdate.Add(existingToken);
                }
            }

            var dbTasks = new List<Task>();

            if (toCreate.Any())
            {
                dbTasks.Add(Task.Run(async () =>
                {
                    await _tokensManager.CreateManyAsync(toCreate.ToArray());
                }));
            }

            if (toUpdate.Any())
            {
                dbTasks.Add(Task.Run(async () =>
                {
                    await _tokensManager.BlukUpdateManyAsync(toUpdate, true);
                }));
            }

            var doneTxnHashes = doneTxns.Select(t => t.Hash).Distinct().ToArray();
            dbTasks.Add(Task.Run(async () =>
            {
                var update = Builders<Txn>.Update.Set(t => t.ApprovalDone, true);
                var updateResult = await _txnsManager.UpdateManyAsync(t => doneTxnHashes.Contains(t.Hash) && !t.ApprovalDone && t.Receipt != null && t.Receipt.Succeeded, update);

                Console.WriteLine($"updated 'ApprovalDone' for {updateResult.ModifiedCount} txns");
            }));

            if (dbTasks.Any())
            {
                await Task.WhenAll(dbTasks);
            }

            //Console.WriteLine($"created {toCreate.Count} and updated {toUpdate.Count} in {(DateTime.UtcNow - now).TotalSeconds} seconds");
            if (toCreate.Any()) Console.WriteLine($"created {toCreate.Count} : {string.Join(", ", toCreate.OrderBy(_ => _.Name).Select(_ => $"{_.DisplayName}[{_.ApproveTxns.Length}/{_.ComputedScore}]"))}");
            if (toUpdate.Any()) Console.WriteLine($"updated {toUpdate.Count} : {string.Join(", ", toUpdate.OrderBy(_ => _.Name).Select(_ => $"{_.DisplayName}[{_.ApproveTxns.Length}/{_.ComputedScore}]"))}");
            Console.WriteLine($"elapsed time: {(DateTime.UtcNow - now).TotalSeconds} seconds");
            //var tokenScores = await _tokensManager.GetTokenScoresAsync(tokenAddresses);
            //Console.WriteLine($"scores : {string.Join(", ", tokenScores.OrderBy(_ => _.Token.Name).Select(_ => $"{_.Token.DisplayName}[{_.Score}]"))}");
            Console.WriteLine("---------------------------------------------------------------");

            //await HotTokensInfoAsync();
        }

        private async Task ApprovedTokensAsync()
        {
            await ActionsHelper.LoopAsync(ApprovedTokensLoop, 222);
        }

        #region internals

        private readonly TxnsManager _txnsManager = null;
        private readonly TokensManager _tokensManager = null;
        private readonly IgnoredTokensManager _ignoredTokensManager = null;
        private readonly EthService _ethService = null;

        public ApprovedTokensApp(TxnsManager txnsManager, TokensManager tokensManager, IgnoredTokensManager ignoredTokensManager, EthService ethService)
        {
            _txnsManager = txnsManager;
            _tokensManager = tokensManager;
            _ignoredTokensManager = ignoredTokensManager;
            _ethService = ethService;
        }

        #endregion
    }
}
