using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Osmos.Business.DataInfos
{
    internal class DataInfosApp: BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { GetInfosAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        public async Task GetInfosLoop() {
            var result = await _ethService.GetBlockchainInfosAsync();
            var current = result.Current.ToBlockInfos();
            var previous = result.Previous.ToBlockInfos();

            var dataInfo = await _dataInfosManager.GetOneAsync();
            if (dataInfo == null)
            {
                dataInfo = new DataInfo
                {
                    Id = "1",
                    CurrentBlock = current,
                    PreviousBlock = previous,
                    GasFees = result.GasFees
                };

                dataInfo.PreviousBlock.SucceededTxnsCount = result.Receipts?.Count(_ => _.Succeeded()) ?? 0;
                dataInfo.PreviousBlock.FailedTxnsCount = result?.Receipts.Count(_ => !_.Succeeded()) ?? 0;

                await _dataInfosManager.CreateOneAsync(dataInfo);
            }
            else {

                dataInfo.CurrentBlock = current;
                dataInfo.PreviousBlock = previous;
                dataInfo.GasFees = result.GasFees;
                dataInfo.UpdatedDate = DateTime.UtcNow;

                dataInfo.PreviousBlock.SucceededTxnsCount = result.Receipts?.Count(_ => _.Succeeded()) ?? 0;
                dataInfo.PreviousBlock.FailedTxnsCount = result?.Receipts.Count(_ => !_.Succeeded()) ?? 0;

                await _dataInfosManager.UpdateOneAsync(dataInfo);
            }

            Console.WriteLine($"current block: {current.Number}");
        }

        public async Task GetInfosAsync() {
            await ActionsHelper.LoopAsync(GetInfosLoop, 1000);
        }

        #region internals

        private readonly DataInfosManager _dataInfosManager = null;
        private readonly EthService _ethService = null;

        public DataInfosApp(
            DataInfosManager dataInfosManager,
            EthService ethService)
        {
            _dataInfosManager = dataInfosManager;
            _ethService = ethService;
        }

        #endregion
    }

    public static class BlockExtensions {
        public static BlockInfos ToBlockInfos(this BlockWithTransactions block) {
            return new BlockInfos {
                Size = block.Size?.ToString(),
                BaseFeePerGas = block.BaseFeePerGas?.ToString(),
                Difficulty = block.Difficulty?.ToString(),
                GasLimit = block.GasLimit?.ToString(),
                GasUsed = block.GasUsed?.ToString(),
                Number = block.Number?.ToString(),
                Date = long.Parse(block.Timestamp?.ToString()).ToDateTime(),
                TotalDifficulty = block.TotalDifficulty?.ToString(),
                TxnsCount = block.Transactions?.Count() ?? 0
            };
        }
    }
}
