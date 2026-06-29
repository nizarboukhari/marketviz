using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Workers.PromotedTokens
{
    internal class PromotedTokensApp : BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { PromotedTokensAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private async Task PromotedTokensLoop()
        {
            var now = DateTime.UtcNow;

            var promotedTokens = await _promotedTokensManager.GetManyAsync();
            Console.WriteLine($"got {promotedTokens.Length} promoted tokens");

            var tokenAddresses = promotedTokens.Select(_ => _.Id).ToArray();
            var existingTokens = await _tokensManager.GetManyAsync(tk => tokenAddresses.Contains(tk.Address));

            var toCreate = new List<Token>();
            var toUpdate = new List<Token>();

            var allPairs = await _ethService.GetPairDatasUnlimitedAsync(tokenAddresses);
            foreach (var address in tokenAddresses)
            {
                var existingToken = existingTokens.FirstOrDefault(tk => tk.Address == address);

                string name = existingToken?.Name;
                string symbol = existingToken?.Symbol;
                string owner = existingToken?.Owner;
                string pair = existingToken?.PairAddress;
                var pairs = existingToken?.Pairs;

                TokenInfo tokenInfo = existingToken?.TokenInfo;
                List<Holder> holders = existingToken?.Top100Holders;

                TokenSourceCode sourceCode = existingToken?.SourceCode;
                Pair pairTradingData = existingToken?.PairTradingData;

                pairs = allPairs.Where(_ => _.BaseToken?.Address?.ToLower() == address || _.QuoteToken?.Address?.ToLower() == address).ToArray();

                if (!pairs.Any())
                {
                    ;
                }

                var tasks = new List<Task> {
                            Task.Run(async () => {
                                tokenInfo = await _ethService.GetTokenInfoSmoothAsync(address);
                            }),
                            Task.Run(async () => {
                                holders = await _ethService.GetTopTokenHoldersSmoothAsync(address, 100);
                            })
                        };

                if (sourceCode == null || sourceCode.MissingSourceCode() || sourceCode.ABI == _ethService.Settings.DefautltABI)
                {
                    sourceCode = await _ethService.GetSourceCodeSmoothAsync(address);
                }

                string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : _ethService.Settings.DefautltABI;
                var tokenContract = _ethService.GetContractWithABISmooth(address, abi);

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

                    if (pair == null && !pairs.Any())
                    {
                        var uniV2FactoryContract = _ethService.GetFactoryContract();
                        if (uniV2FactoryContract != null)
                        {
                            tasks.Add(Task.Run(async () => {
                                pair = await _ethService.GetPair(address, _ethService.Settings.WETH, uniV2FactoryContract);
                            }));
                        }
                    }
                }

                await Task.WhenAll(tasks);

                if (pair != null)
                {
                    pairTradingData = await _ethService.GetPairDataAsync(pair);
                    pairs = new Pair[] {
                        pairTradingData
                    };
                }

                if (pairTradingData == null && pairs.Any()) {
                    pairTradingData = pairs[0];
                }

                double? price = null;
                try
                {
                    if (pairTradingData != null && double.TryParse(pairTradingData.PriceUsd, out double _price))
                    {
                        price = _price;
                    }
                }
                catch { }
                var tokenPrice = new TokenPrice
                {
                    Value = price
                };

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
                        Prices = new List<TokenPrice> {
                            tokenPrice
                        }
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

                    if (existingToken.Prices.LastOrDefault() == null)
                    {
                        existingToken.Prices = new List<TokenPrice> {
                                    tokenPrice
                                };
                    }
                    else if (existingToken.Prices.LastOrDefault().Value != price)
                    {
                        existingToken.Prices.Add(tokenPrice);
                    }

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
                    await _tokensManager.BlukUpdateManyAsync(toUpdate, false);
                }));
            }

            if (dbTasks.Any())
            {
                await Task.WhenAll(dbTasks);
            }

            //Console.WriteLine($"created {toCreate.Count} and updated {toUpdate.Count} in {(DateTime.UtcNow - now).TotalSeconds} seconds");
            if (toCreate.Any()) Console.WriteLine($"created {toCreate.Count} : {string.Join(", ", toCreate.OrderBy(_ => _.Name).Select(_ => $"{_.DisplayName}"))}");
            if (toUpdate.Any()) Console.WriteLine($"updated {toUpdate.Count} : {string.Join(", ", toUpdate.OrderBy(_ => _.Name).Select(_ => $"{_.DisplayName}"))}");
            Console.WriteLine($"elapsed time: {(DateTime.UtcNow - now).TotalSeconds} seconds");
        }

        private async Task PromotedTokensAsync()
        {
            await ActionsHelper.LoopAsync(PromotedTokensLoop, 5 * 60000);
        }

        #region internals

        private readonly TokensManager _tokensManager = null;
        private readonly PromotedTokensManager _promotedTokensManager = null;
        private readonly EthService _ethService = null;

        public PromotedTokensApp(TokensManager tokensManager, PromotedTokensManager promotedTokensManager, EthService ethService)
        {
            _tokensManager = tokensManager;
            _promotedTokensManager = promotedTokensManager;
            _ethService = ethService;
        }

        #endregion
    }
}
