using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.PendingTxns
{
    internal class PendingTxnsApp : BaseApp
    {
        protected override async Task RunAsync()
        {

            var functions = new Func<Task>[] { GetPendingTxnsAsync, SetTxnsAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private readonly object _txnsLock = new object();
        private List<Txn> _txns = new List<Txn>();

        private async Task GetPendingTxnsLoop()
        {
            var pendingTxns = await _ethService.GetPendingTxnsAsync();

            lock (_txnsLock)
            {
                _txns.AddRange(pendingTxns);
                Console.WriteLine($"{_txns.Count} new pending txns");
            }
        }

        private async Task GetPendingTxnsAsync()
        {
            await ActionsHelper.LoopAsync(GetPendingTxnsLoop,  222);
        }

        private async Task SetTxnsLoop()
        {

            var now = DateTime.UtcNow;
            Txn[] currentTxns = null;
            lock (_txnsLock)
            {
                if (!_txns.Any()) return;

                currentTxns = _txns.GroupBy(t => t.Hash)
                                    .Select(g => g.OrderBy(t => t.BlockNumber).Last())
                                    .OrderBy(t => t.BlockNumber)
                                    .ToArray();

                _txns = new List<Txn>();
            }

            var hashes = currentTxns.Select(t => t.Hash).ToArray();
            var existingTxns = await _txnsManager.GetManyAsync(t => hashes.Contains(t.Hash));

            var toCreate = new List<Txn>();
            var toUpdate = new List<Txn>();

            foreach (var txn in currentTxns)
            {
                ActionsHelper.SmoothRun(() => {
                    var existing = existingTxns.FirstOrDefault(t => t.Hash == txn.Hash);
                    if (existing == null) toCreate.Add(txn);
                    else if (existing.BlockNumber.CompareTo(txn.BlockNumber) < 0)
                    {
                        toUpdate.Add(txn);
                    }
                });
            }

            foreach (var txn in toCreate)
            {
                ActionsHelper.SmoothRun(() => {
                    if (FunctionSignaturesHelper.IsNotFound(txn.FunctionSignature)) return;

                    string functionName = FunctionSignaturesHelper.GetFunctionName(txn.FunctionSignature);
                    if (functionName == null) return;

                    txn.FunctionName = functionName;
                });
            }

            await Task.WhenAll(new Task[] {
                        Task.Run(async () => {
                            if(!toCreate.Any()) return;

                            await _txnsManager.CreateManyAsync(toCreate.ToArray());
                        }),
                        Task.Run(async () => {
                            if(!toUpdate.Any()) return;

                            await _txnsManager.BlukUpdateBlockNumberAsync(toUpdate);
                        })
                    });

            Console.WriteLine($"created {toCreate.Count} and updated {toUpdate.Count} txns in {(DateTime.UtcNow - now).TotalSeconds} seconds");

        }

        private async Task SetTxnsAsync()
        {
            await ActionsHelper.LoopAsync(SetTxnsLoop, 222);
        }

        class BlockGroup
        {
            public string BlockNumber { get; set; }
            public Txn[] Txns { get; set; }
        }

        #region internals

        private readonly TxnsManager _txnsManager = null;
        private readonly EthService _ethService = null;

        public PendingTxnsApp(IOptions<FunctionSignatureSettings> functionSignatureSettings, TxnsManager txnsManager, EthService ethService)
        {
            _txnsManager = txnsManager;

            FunctionSignaturesHelper.Init(functionSignatureSettings.Value.FolderName);
            
            _ethService = ethService;

        }

        #endregion
    }
}
