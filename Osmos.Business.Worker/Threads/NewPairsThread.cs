using Nethereum.Contracts;
using Nethereum.Web3;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Models;
using Osmos.Business.Worker.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Threads
{
    public static class NewPairsThread
    {
        public static void Start(TokensManager tokensManager)
        {
            var setLoop = new Thread(new ThreadStart(() => GetNewPairsLoop(tokensManager)));

            setLoop.Start();
        }

        private static Logger _logger = new Logger("NewPairsThread", true);

        private static async void GetNewPairsLoop(TokensManager tokensManager)
        {

            var tokenPairList = new List<TokenPair>();
            var web3Ws = new Web3(Settings.EthWsURL);

            var transferEventHandler = web3Ws.Eth.GetEvent<PairCreatedEventDTO>(Settings.uniV2Factory);
            var filterInputCreatedPairEvents = transferEventHandler.CreateFilterInput();
            var filterCreatedPairEvents = await transferEventHandler.CreateFilterAsync(filterInputCreatedPairEvents);

            do
            {
                try
                {
                    var result = await transferEventHandler.GetFilterChangesAsync(filterCreatedPairEvents);

                    foreach (var eventLog in result)
                    {
                        string pair = eventLog.Event.Pair;

                        tokenPairList.Add(
                            new TokenPair
                            {
                                Token0 = eventLog.Event.Token0,
                                Token1 = eventLog.Event.Token1,
                                Address = pair
                            }
                        );

                        // Uncomment this to add to our token list instead
                        // Add bool fromNewPairs
                        string address = null;

                        if (eventLog.Event.Token1 == Settings.WETH || eventLog.Event.Token1 == Settings.USDC)
                            address = eventLog.Event.Token0;
                        else
                            address = eventLog.Event.Token1;

                        var token = await tokensManager.GetOneAsync(t => t.Address == address);

                        if (token == null)
                        {
                            string name = null;
                            string symbol = null;
                            string owner = null;
                            //string pair = null;

                            var sourceCode = await TokenService.GetSourceCodeAsync(address);

                            string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : Settings.defautltABI;
                            var tokenContract = TokenService.GetContractWithABI(address, abi);

                            var uniV2FactoryContract = TokenService.GetFactoryContract(Settings.uniV2Factory);

                            // remark: should we throw if only one of them fail??
                            await Task.WhenAll(new Task[] {
                                Task.Run(async () => {
                                    name = await TokenService.GetTokenNameAsync(tokenContract);
                                }),
                                Task.Run(async () => {
                                    symbol = await TokenService.GetTokenSymbolAsync(tokenContract);
                                }),
                                Task.Run(async () => {
                                    owner = await TokenService.GetTokenOwnerAsync(tokenContract);
                                })
                            });

                            token = new Token
                            {
                                Address = address,
                                Name = name,
                                Symbol = symbol,
                                Owner = owner,
                                PairAddress = pair,
                                SourceCode = sourceCode
                            };

                            await tokensManager.CreateOneAsync(token);

                            _logger.Write($"created new token : {address}");
                        }
                        else
                        {
                            Contract tokenContract = null;

                            if (token.Owner != "0x0000000000000000000000000000000000000000")
                            {
                                var sourceCode = await TokenService.GetSourceCodeAsync(address);

                                string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : Settings.defautltABI;
                                tokenContract = TokenService.GetContractWithABI(address, abi);

                                token.SourceCode = sourceCode;

                                token.Owner = await TokenService.GetTokenOwnerAsync(tokenContract);
                            }

                            if (tokenContract != null && (token.SourceCode == null || token.SourceCode.MissingSourceCode()))
                            {
                                var sourceCode = await TokenService.GetSourceCodeAsync(address);

                                if (sourceCode != null && !sourceCode.MissingSourceCode())
                                {
                                    token.SourceCode = sourceCode;
                                }
                            }

                            await tokensManager.UpdateOneExAsync(token);

                            _logger.Write($"updated token : {address}");
                        }

                    }
                }
                catch (Exception ex)
                {
                    ;
                }

                Thread.Sleep(500);

            } while (true);
        }

    }
}
