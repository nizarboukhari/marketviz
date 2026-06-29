using Microsoft.AspNetCore.SignalR.Client;
using MongoDB.Driver;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Workers.GameNotifier
{
    internal class GameNotifierApp : BaseApp
    {
        protected override async Task RunAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://mrktviz-api.azurewebsites.net/appHub")
                //.WithUrl("http://localhost:5000/appHub")
                .Build();

            _connection.On("Pong", () =>
            {
                Console.WriteLine("Pong");
            });

            await _connection.StartAsync();

            var functions = new Func<Task>[] { Async };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        public async Task Loop()
        {
            await _connection.SmoothInvokeAsync("Ping");

            var currentBlock = await _ethService.GetCurrentBlockAsync();
            var currentBlockNumber = currentBlock.Number;
            if (_lastBlockNumber == currentBlockNumber)
            {
                return;
            }

            _lastBlockNumber = currentBlockNumber;

            HotToken[] hotTokens = null;
            BlockWithTransactions latestBlock = null;
            HexBigInteger gasFees = null;
            double ethPrice = -1;

            var getTasks = new Task[] {
                Task.Run(async () => {
                    hotTokens = await _hotTokensManager.GetManyAsync();

                    hotTokens = hotTokens.OrderByDescending(_ => _.Score).ToArray();
                }),
                 Task.Run(async () => {
                    do
                    {
                        await Task.Delay(222);
                        latestBlock = await _ethService.GetLatestBlockAsync();
                    } while (BigInteger.Subtract(currentBlockNumber.Value, latestBlock.Number.Value) > 1);
                }),
                  Task.Run(async () => {
                    gasFees = await _ethService.GetGasFeesAsync();
                }),
                  Task.Run(async () => {
                    ethPrice = await _ethService.GetEthPriceAsync();
                })
            };

            await Task.WhenAll(getTasks);

            var hotTokenItems = hotTokens
                .Select((item, index) => new HotTokenItem
                {
                    Rank = index + 1,
                    Address = item.Token.Address,
                    Score = item.Score,
                    PriceNative = item.Token.Pairs.FirstOrDefault()?.PriceNative ?? "0",
                    PriceUsd = item.Token.Pairs.FirstOrDefault()?.PriceUsd ?? "0",
                    Name = item.Token.Name,
                    Symbol = item.Token.Symbol,
                    DisplayName = item.Token.DisplayName,
                    RenouncedOwnership = item.Token.Owner == "0x0000000000000000000000000000000000000000",
                    SourceCodeExists = !(item.Token.SourceCode == null || item.Token.SourceCode.Missing)
                })
                .ToArray();

            var now = DateTime.UtcNow;
            var computedTime = (now - _date).TotalSeconds;
            
            var gameData = new GameData
            {
                Id = latestBlock.Number.ToString(),
                BaseFeePerGas = latestBlock.BaseFeePerGas.ToString(),
                Date = long.Parse(latestBlock.Timestamp?.ToString()).ToDateTime(),
                Difficulty = latestBlock.Difficulty.ToString(),
                GasLimit = latestBlock.GasLimit.ToString(),
                GasUsed = latestBlock.GasUsed.ToString(),
                Size = latestBlock.Size.ToString(),
                TotalDifficulty = latestBlock.TotalDifficulty?.ToString(),
                TxnsCount = latestBlock.Transactions?.Count() ?? 0,
                GasFees = Convert.ToDecimal(gasFees.Value.ToString()),
                ComputeTime = computedTime,
                EthPrice = ethPrice,
                HotTokens = hotTokenItems
            };

            gameData = await _gameDataManager.CreateOneAsync(gameData);

            var pendingBets = await _betsManager.GetManyAsync(_ => _.Pending);

            var blocks = pendingBets.SelectMany(obj => new[] { obj.TargetBlock, obj.SourceBlock }).Distinct().ToArray();
            var gameDataList = await _gameDataManager.GetManyAsync(_ => blocks.Contains(_.Id));

            var groupedByWallets = pendingBets.GroupBy(_ => _.Wallet).ToList();

            foreach (var walletBets in groupedByWallets)
            {
                foreach (var pendingBet in walletBets)
                {
                    pendingBet.Pending = false;

                    var targetGameData = gameDataList.FirstOrDefault(_ => _.Id == pendingBet.TargetBlock);
                    var sourceGameData = gameDataList.FirstOrDefault(_ => _.Id == pendingBet.SourceBlock);

                    if (targetGameData == null || sourceGameData == null)
                    {
                        continue;
                    }
                    else if (pendingBet.Item == "txnsCount")
                    {
                        pendingBet.SoloSuccess = pendingBet.Up ? targetGameData.TxnsCount > sourceGameData.TxnsCount : targetGameData.TxnsCount < sourceGameData.TxnsCount;
                    }
                    else if (pendingBet.Item == "gasFees")
                    {
                        pendingBet.SoloSuccess = pendingBet.Up ? targetGameData.GasFees > sourceGameData.GasFees : targetGameData.GasFees < sourceGameData.GasFees;
                    }
                    else if (pendingBet.Item == "gasUsed")
                    {
                        pendingBet.SoloSuccess = pendingBet.Up ?
                            BigInteger.Parse(targetGameData.GasUsed) > BigInteger.Parse(sourceGameData.GasUsed) : BigInteger.Parse(targetGameData.GasUsed) < BigInteger.Parse(sourceGameData.GasUsed);
                    }
                    else if (pendingBet.Item == "size")
                    {
                        pendingBet.SoloSuccess = pendingBet.Up ?
                            BigInteger.Parse(targetGameData.Size) > BigInteger.Parse(sourceGameData.Size) : BigInteger.Parse(targetGameData.Size) < BigInteger.Parse(sourceGameData.Size);
                    }
                    else if (pendingBet.Item == "computeTime")
                    {
                        pendingBet.SoloSuccess = pendingBet.Up ?
                            targetGameData.ComputeTime > sourceGameData.ComputeTime : targetGameData.ComputeTime < sourceGameData.ComputeTime;
                    }
                    else if (pendingBet.Item == "ethPrice")
                    {
                        pendingBet.SoloSuccess = pendingBet.Up ?
                            targetGameData.EthPrice > sourceGameData.EthPrice : targetGameData.EthPrice < sourceGameData.EthPrice;
                    }
                    else if (pendingBet.Item == "token")
                    {
                        var sourceToken = sourceGameData.HotTokens.FirstOrDefault(_ => _.Address == pendingBet.HotToken.Address);
                        var targetToken = targetGameData.HotTokens.FirstOrDefault(_ => _.Address == pendingBet.HotToken.Address);

                        if (sourceToken == null || targetToken == null) pendingBet.SoloSuccess = false;
                        else
                        {
                            pendingBet.SoloSuccess = pendingBet.Up ?
                                decimal.Parse(targetToken.PriceUsd) > decimal.Parse(sourceToken.PriceUsd) : decimal.Parse(targetToken.PriceUsd) < decimal.Parse(sourceToken.PriceUsd);
                        }
                    }
                    else
                    {
                        pendingBet.SoloSuccess = false;
                    }

                    pendingBet.Success = pendingBet.SoloSuccess;

                    await _betsManager.UpdateOneAsync(pendingBet);
                }

                var grouped = walletBets.Where(_ => _.SoloSuccess != null).GroupBy(_ => string.Join("|", _.GroupBetIds));
                foreach (var group in grouped)
                {
                    if (group.Count() > 1)
                    {
                        var update = Builders<Bet>.Update.Combine(Builders<Bet>.Update.Set(_ => _.Success,
                        !group.Any(_ => (bool)!_.SoloSuccess)),
                        Builders<Bet>.Update.Set(_ => _.Pending, false));
                        await _betsManager.UpdateManyAsync(group.ToArray(), update);
                    }
                }
            }

            Console.WriteLine(JsonConvert.SerializeObject(blocks));

            await _connection.SmoothInvokeAsync("GameData", gameData);

            Console.WriteLine($"current block: {currentBlockNumber.Value} | gas : {gasFees.Value} | took {computedTime} sec");

            //Console.WriteLine($"{JsonConvert.SerializeObject(gameData)}");
            _date = now;
        }

        public async Task Async()
        {
            await ActionsHelper.LoopAsync(Loop, 1000);
        }

        #region internals

        private HubConnection _connection = null;
        private readonly HotTokensManager _hotTokensManager = null;
        private readonly GameDataManager _gameDataManager = null;
        private readonly BetsManager _betsManager = null;
        private readonly EthService _ethService = null;
        private HexBigInteger _lastBlockNumber = null;
        private DateTime _date = DateTime.UtcNow;

        public GameNotifierApp(
            HotTokensManager hotTokensManager,
            GameDataManager gameDataManager,
            BetsManager betsManager,
            EthService ethService)
        {
            _hotTokensManager = hotTokensManager;
            _gameDataManager = gameDataManager;
            _betsManager = betsManager;
            _ethService = ethService;
        }

        #endregion
    }
}
