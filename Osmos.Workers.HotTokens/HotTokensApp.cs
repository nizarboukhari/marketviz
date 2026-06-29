using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.HotTokens
{
    internal class HotTokensApp : BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { HotTokensInfoAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private async Task HotTokensInfoLoop()
        {
            var now = DateTime.UtcNow;

            var hotTokensResult = await _tokensManager.GetHotTokensAsync();

            Console.WriteLine($"{string.Join(", ", hotTokensResult.OrderBy(_ => _.Score).Select(_ => $"{_.Token.DisplayName}[{_.Score}]"))}");

            var toUpdate = new List<Token>();

            var allPairs = await _ethService.GetPairDatasUnlimitedAsync(hotTokensResult.Select(_ => _.Token.Address));
            foreach (var item in hotTokensResult)
            {
                var token = item.Token;

                if (token == null) continue;

                string name = token.Name;
                string symbol = token.Symbol;
                string owner = token.Owner;
                var pairs = token?.Pairs;

                TokenInfo tokenInfo = token.TokenInfo;
                List<Holder> holders = token.Top100Holders;

                TokenSourceCode sourceCode = token.SourceCode;
                Pair pairTradingData = token.PairTradingData;

                if (sourceCode == null || sourceCode.MissingSourceCode() || sourceCode.ABI == _ethService.Settings.DefautltABI)
                {
                    sourceCode = await _ethService.GetSourceCodeSmoothAsync(token.Address);
                }

                string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : _ethService.Settings.DefautltABI;
                var tokenContract = _ethService.GetContractWithABISmooth(token.Address, abi);

                var tasks = new List<Task> {
                            Task.Run(async () => {
                                tokenInfo = await _ethService.GetTokenInfoSmoothAsync(token.Address);
                            }),
                            Task.Run(async () => {
                                holders = await _ethService.GetTopTokenHoldersSmoothAsync(token.Address, 100);
                            })
                        };

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
                        owner = await _ethService.GetTokenOwnerAsync(tokenContract);
                    }));
                }

                await Task.WhenAll(tasks);

                pairs = allPairs.Where(_ => _.BaseToken?.Address?.ToLower() == token.Address || _.QuoteToken?.Address?.ToLower() == token.Address).ToArray();

                if (!pairs.Any())
                {
                    ;
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

                token.Name = name;
                token.Symbol = symbol;
                token.Owner = owner;
                token.SourceCode = sourceCode;
                token.TokenInfo = tokenInfo;
                token.Top100Holders = holders;
                token.Pairs = pairs;

                if (token.Prices.LastOrDefault() == null)
                {
                    token.Prices = new List<TokenPrice> {
                                    tokenPrice
                                };
                }
                else if (token.Prices.LastOrDefault().Value != price)
                {
                    token.Prices.Add(tokenPrice);
                }

                toUpdate.Add(token);

            }

            var previousHotTokens = await _hotTokensManager.GetManyAsync();

            await Task.WhenAll(new Task[] {
               Task.Run(async () => {
                    await _tokensManager.BlukUpdateManyAsync(toUpdate, false);
               }),
               Task.Run(async () => {
                    var hotTokens = new List<HotToken>();
                    int i = 1;
                    foreach (var item in hotTokensResult.OrderByDescending(_ => _.Score))
                    {
                        var hotToken = new HotToken
                        {
                            Id = i++.ToString(),
                            Score = item.Score,
                            Token = item.Token,
                            ScoreDates = new List<ScoreDate>{
                                new ScoreDate{
                                    Date = now,
                                    Score = item.Score
                                }
                            }
                        };

                        var previousHotToken = previousHotTokens.FirstOrDefault(_ => _.Token.Address == item.Token.Address);
                       if(previousHotToken != null && previousHotToken.ScoreDates.Any()){
                           hotToken.ScoreDates.AddRange(previousHotToken.ScoreDates);
                           hotToken.ScoreDates = hotToken.ScoreDates.OrderBy(_ => _.Date).ToList();
                       }

                        hotTokens.Add(hotToken);
                    }

                    //await _hotTokensManager.CreateManyAsync(hotTokens.ToArray());
                    await _hotTokensManager.BlukUpdateManyAsync(hotTokens);
               }),
            });

            Console.WriteLine($"updated hot tokens in {(DateTime.UtcNow - now).TotalSeconds} seconds");
        }

        private async Task HotTokensInfoAsync()
        {
            await ActionsHelper.LoopAsync(HotTokensInfoLoop, 1000);
        }

        #region internals

        private readonly TokensManager _tokensManager = null;
        private readonly HotTokensManager _hotTokensManager = null;
        private readonly EthService _ethService = null;

        public HotTokensApp(TokensManager tokensManager, HotTokensManager hotTokensManager, EthService ethService)
        {
            _tokensManager = tokensManager;
            _hotTokensManager = hotTokensManager;
            _ethService = ethService;
        }

        #endregion
    }
}
