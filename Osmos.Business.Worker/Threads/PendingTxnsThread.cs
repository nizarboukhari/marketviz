using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Threads
{
    public static class PendingTxnsThread
    {
        public static void Start(TxnsManager txnsManager)
        {
            var getLoop = new Thread(new ThreadStart(() => GetPendingTxnsLoop()));
            var setLoop = new Thread(new ThreadStart(() => SetTxnsLoop(txnsManager)));

            getLoop.Start();
            setLoop.Start();
        }

        private static readonly object _txnsLock = new object();
        private static List<Txn> _txns = new List<Txn>();

        private static Logger _logger = new Logger("PendingTxnsThread", false);

        private static async void GetPendingTxnsLoop()
        {
            do
            {
                try
                {
                    var pendingTxns = await MemepoolService.GetPendingTxns();

                    lock (_txnsLock)
                    {
                        _txns.AddRange(pendingTxns);
                        _logger.Write($"got {_txns.Count} new pending txns");
                    }
                }
                catch (Exception ex)
                {

                }

                Thread.Sleep(222);

            } while (true);
        }

        private static async void SetTxnsLoop(TxnsManager txnsManager)
        {
            do
            {
                //Thread.Sleep(1000);

                var now = DateTime.UtcNow;
                try
                {
                    Txn[] currentTxns = null;
                    lock (_txnsLock)
                    {
                        if (!_txns.Any()) continue;

                        currentTxns = _txns.GroupBy(t => t.Hash)
                                            .Select(g => g.OrderBy(t => t.BlockNumber).Last())
                                            .OrderBy(t => t.BlockNumber)
                                            .ToArray();

                        _txns = new List<Txn>();
                    }

                    var hashes = currentTxns.Select(t => t.Hash).ToArray();
                    var existingTxns = await txnsManager.GetManyAsync(t => hashes.Contains(t.Hash));

                    var toCreate = new List<Txn>();
                    var toUpdate = new List<Txn>();

                    foreach (var txn in currentTxns)
                    {
                        var existing = existingTxns.FirstOrDefault(t => t.Hash == txn.Hash);
                        if (existing == null) toCreate.Add(txn);
                        else if (existing.BlockNumber.CompareTo(txn.BlockNumber) < 0)
                        {
                            toUpdate.Add(txn);
                        }
                    }

                    foreach (var txn in toCreate)
                    {
                        if (FunctionSignaturesHelper.IsNotFound(txn.FunctionSignature)) continue;

                        string functionName = FunctionSignaturesHelper.GetFunctionName(txn.FunctionSignature);
                        if (functionName == null) continue;

                        txn.FunctionName = functionName;
                    }

                    await Task.WhenAll(new Task[] {
                        Task.Run(async () => {
                            if(!toCreate.Any()) return;

                            await txnsManager.CreateManyAsync(toCreate.ToArray());
                        }),
                        Task.Run(async () => {
                            if(!toUpdate.Any()) return;

                            var grouped = toUpdate.GroupBy(t => t.BlockNumber)
                                            .Select(g => new BlockGroup{
                                                BlockNumber = g.Key,
                                                Txns = g.ToArray()
                                            });

                            await Task.WhenAll(grouped.Select(g => Task.Run(async () => {
                                var update = Builders<Txn>.Update.Set(t => t.BlockNumber, g.BlockNumber);
                                await txnsManager.UpdateManyAsync(g.Txns, update);
                            })));
                        })
                    });
                }
                catch (Exception ex)
                {
                    ;
                }
            } while (true);
        }

        class BlockGroup {
            public string BlockNumber { get; set; }
            public Txn[] Txns { get; set; }
        }
    }
}
