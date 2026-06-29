using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Models;
using Osmos.Business.Worker.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Threads
{
    public static class ApprovedTokensThread
    {
        public static void Start(TokensManager tokensManager, TxnsManager txnsManager, IgnoredTokensManager ignoredTokensManager)
        {
            var loop = new Thread(new ThreadStart(() => ApprovedTokensLoop(tokensManager, txnsManager, ignoredTokensManager)));
            loop.Start();
        }

        private static Logger _logger = new Logger("ApprovedTokensThread", true);

        private static List<string> _ignoreTokenAddresses = new List<string>();

        private static async void ApprovedTokensLoop(TokensManager tokensManager, TxnsManager txnsManager, IgnoredTokensManager ignoredTokensManager)
        {
            _ignoreTokenAddresses = await ignoredTokensManager.GetAllAsync();

            do
            {
                try
                {
                    var now = DateTime.UtcNow;

                    var txns = await txnsManager.GetManyAsync(t => t.IsApprove && !t.GotTokenInfo);

                    var addresses = txns.Select(t => t.To.ToLower()).Distinct().Where(a => !_ignoreTokenAddresses.Contains(a.ToLower())).ToArray();

                    var existingTokens = await tokensManager.GetManyAsync(tk => addresses.Contains(tk.Address));

                    var toCreate = new List<Token>();
                    var toUpdate = new List<Token>();

                    var savedTokenAddresses = new List<string>();

                    _logger.Write($"checking {addresses.Count()} addresses");

                    // remark: check if the use of try is justified
                    foreach (var address in addresses)
                    {
                        var token = existingTokens.FirstOrDefault(tk => address.Contains(tk.Address));

                        string name = token?.Name;
                        string symbol = token?.Symbol;
                        string owner = token?.Owner;
                        string pair = token?.PairAddress;

                        TokenInfo tokenInfo = token?.TokenInfo;
                        List<Holder> holders = token?.Top100Holders;

                        TokenSourceCode sourceCode = token?.SourceCode;
                        Pair pairTradingData = token?.PairTradingData;

                        DateTime? lastInfoDate = token?.LastInfoDate;

                        if (token == null || (token != null && (token.LastInfoDate == null || token.LastInfoDate != null && (now - token.LastInfoDate.Value).TotalSeconds > 12))) {
                            if (sourceCode == null || sourceCode.MissingSourceCode() || sourceCode.ABI == Settings.defautltABI)
                            {
                                sourceCode = await TokenService.TryGetSourceCodeAsync(address);
                            }

                            string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : Settings.defautltABI;
                            var tokenContract = TokenService.TryGetContractWithABI(address, abi);

                            var tasks = new List<Task> {
                                Task.Run(async () => {
                                    tokenInfo = await TokenInfoService.TryGetBlockChainData(address);
                                }),
                                Task.Run(async () => {
                                    holders = await TokenInfoService.TryGetTopTokenHolders(address, 100);
                                })
                            };

                            if (tokenContract != null)
                            {
                                if (name == null)
                                {
                                    tasks.Add(Task.Run(async () => {
                                        name = await TokenService.TryGetTokenNameAsync(tokenContract);
                                    }));
                                }
                                if (symbol == null)
                                {
                                    tasks.Add(Task.Run(async () => {
                                        symbol = await TokenService.TryGetTokenSymbolAsync(tokenContract);
                                    }));
                                }

                                tasks.Add(Task.Run(async () => {
                                    owner = await TokenService.TryGetTokenOwnerAsync(tokenContract);
                                }));

                                if (pair == null)
                                {
                                    var uniV2FactoryContract = TokenService.TryGetFactoryContract(Settings.uniV2Factory);
                                    if (uniV2FactoryContract != null)
                                    {
                                        tasks.Add(Task.Run(async () => {
                                            pair = await TokenService.TryGetPair(address, Settings.WETH, uniV2FactoryContract);
                                        }));
                                    }
                                }
                            }

                            await Task.WhenAll(tasks);


                            if (name == null || pair == null) {
                                _ignoreTokenAddresses.Add(address);
                                await ignoredTokensManager.CreateOneAsync(address);
                                continue;
                            }

                            if (pair != null)
                            {
                                pairTradingData = await TradingDataService.TryGetTradingDataAsync(pair);
                            }

                            lastInfoDate = now;
                        }

                        double? price = null;
                        try
                        {
                            if (pairTradingData != null && double.TryParse(pairTradingData.PriceUsd, out double _price)) {
                                price = _price;
                            }
                        }
                        catch{}
                        var tokenPrice = new TokenPrice
                        {
                            Value = price
                        };

                        if (token == null)
                        {
                            token = new Token
                            {
                                Address = address,
                                Name = name,
                                Symbol = symbol,
                                Owner = owner,
                                PairAddress = pair,
                                SourceCode = sourceCode,
                                TokenInfo = tokenInfo,
                                Top100Holders = holders,
                                PairTradingData = pairTradingData,
                                LastInfoDate = lastInfoDate,
                                LastApprovedDate = now,
                                ApprovedDates = new List<DateTime> { now },
                                // remarks: add approvals in the new pair thread??
                                Approvals = 1,
                                Prices = new List<TokenPrice> {
                                    tokenPrice
                                }
                            };

                            toCreate.Add(token);
                        }
                        else
                        {
                            token.Name = name;
                            token.Symbol = symbol;
                            token.Owner = owner;
                            token.SourceCode = sourceCode;
                            token.PairAddress = pair;
                            token.TokenInfo = tokenInfo;
                            token.Top100Holders = holders;
                            token.PairTradingData = pairTradingData;
                            token.Approvals++;
                            token.LastInfoDate = lastInfoDate;

                            if (token.Prices.LastOrDefault() == null)
                            {
                                token.Prices = new List<TokenPrice> {
                                    tokenPrice
                                };
                            }
                            else if(token.Prices.LastOrDefault().Value != price) {
                                token.Prices.Add(tokenPrice);
                            }

                            toUpdate.Add(token);
                        }

                        if (token.Name != null) {
                            savedTokenAddresses.Add(token.Address);
                        }
                    }

                    var dbTasks = new List<Task>();

                    if (toCreate.Any())
                    {
                        dbTasks.Add(Task.Run(async () => {
                            await tokensManager.CreateManyAsync(toCreate.ToArray());
                        }));
                    }

                    if (toUpdate.Any())
                    {
                        //dbTasks.AddRange(toUpdate.Select(token => Task.Run(async () =>
                        //{
                        //    await tokensManager.UpdateOneExAsync(token, true);
                        //})));

                        dbTasks.Add(Task.Run(async () =>
                        {
                            await tokensManager.BlukUpdateManyAsync(toUpdate, true);
                        }));
                    }

                    if (savedTokenAddresses.Any()) {
                        var addressesArray = savedTokenAddresses.Distinct().ToArray();

                        dbTasks.Add(Task.Run(async () => {
                            var update = Builders<Txn>.Update.Set(t => t.GotTokenInfo, true);
                            var updateResult = await txnsManager.UpdateManyAsync(t => addressesArray.Contains(t.To) && !t.GotTokenInfo, update);

                            _logger.Write($"updated 'GotTokenInfo' for {updateResult.ModifiedCount} txns");
                        }));
                    }

                    if (dbTasks.Any())
                    {
                        await Task.WhenAll(dbTasks);
                    }

                    _logger.Write($"created {toCreate.Count} and updated {toUpdate.Count} in {(DateTime.UtcNow - now).TotalSeconds} seconds");

                    Thread.Sleep(250);
                }
                catch (Exception e)
                {
                    ;
                }
            } while (true);
        }
    }
}
