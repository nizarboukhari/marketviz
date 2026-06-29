using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class TxnsManager : MongoDbEntitiesManager<Txn>
    {
        public async Task<BlockTxnsResult[]> GroupTxnsByBlockAsync(int limit = 12)
        {
            var groupDocument = new BsonDocument {
                { "$group", new BsonDocument{
                    { "_id", "$blockNumber"},
                    { "txns", new BsonDocument{ {"$push", "$$ROOT"}} }
                } }
            };

            var sortDocument = new BsonDocument {
                { "$sort", new BsonDocument{ { "_id", -1} } }
            };

            var limitDocument = new BsonDocument { { "$limit", limit } };

            var pipelineDocument = new BsonDocument[] {
                groupDocument,
                sortDocument,
                limitDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<BlockTxnsResult>(pipelineDocument, options).ToListAsync();

            return result.ToArray();
        }

        public async Task<TokenTxnsResult[]> GroupByTokenAsync(int limit = 100)
        {

            var groupDocument = new BsonDocument {
                { "$group", new BsonDocument{
                    { "_id", "$to"},
                    { "count", new BsonDocument{ { "$sum", 1}} }
                } }
            };

            var sortDocument = new BsonDocument {
                { "$sort", new BsonDocument{ { "count", -1} } }
            };

            var limitDocument = new BsonDocument { { "$limit", limit } };

            var pipelineDocument = new BsonDocument[] {
                groupDocument,
                sortDocument,
                limitDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<TokenTxnsResult>(pipelineDocument, options).ToListAsync();

            return result.ToArray();
        }

        public async Task BlukUpdateBlockNumberAsync(IEnumerable<Txn> txns)
        {

            var updates = new List<WriteModel<Txn>>();

            var now = DateTime.UtcNow;

            foreach (var txn in txns)
            {
                txn.LastUpdatedDate = now;
                txn.UpdatedDates.Add(now);

                var filterDefinition = Builders<Txn>.Filter.Eq(t => t.Hash, txn.Hash);

                var updateDefs = new List<UpdateDefinition<Txn>>
                {
                    Builders<Txn>.Update.Set(t => t.BlockNumber, txn.BlockNumber)
                };

                var updateDefinition = Builders<Txn>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<Txn>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public async Task BlukUpdateNamesAndSignaturesAsync(IEnumerable<Txn> txns)
        {

            var updates = new List<WriteModel<Txn>>();

            var now = DateTime.UtcNow;

            foreach (var txn in txns)
            {
                txn.LastUpdatedDate = now;
                txn.UpdatedDates.Add(now);

                var filterDefinition = Builders<Txn>.Filter.Eq(t => t.Hash, txn.Hash);

                var updateDefs = new List<UpdateDefinition<Txn>>
                {
                    Builders<Txn>.Update.Set(t => t.FunctionName, txn.FunctionName),
                    Builders<Txn>.Update.Set(t => t.FunctionSignature, txn.FunctionSignature)
                };

                var updateDefinition = Builders<Txn>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<Txn>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public async Task BlukUpdateManyAsync(IEnumerable<Txn> txns)
        {

            var updates = new List<WriteModel<Txn>>();

            var now = DateTime.UtcNow;

            foreach (var txn in txns)
            {
                txn.LastUpdatedDate = now;
                txn.UpdatedDates.Add(now);

                var filterDefinition = Builders<Txn>.Filter.Eq(t => t.Hash, txn.Hash);

                var updateDefs = new List<UpdateDefinition<Txn>>
                {
                    Builders<Txn>.Update.Set(t => t.BlockNumber, txn.BlockNumber),
                    Builders<Txn>.Update.Set(t => t.Hash, txn.Hash),
                    Builders<Txn>.Update.Set(t => t.From, txn.From),
                    Builders<Txn>.Update.Set(t => t.To, txn.To),
                    Builders<Txn>.Update.Set(t => t.FunctionName, txn.FunctionName),
                    Builders<Txn>.Update.Set(t => t.FunctionSignature, txn.FunctionSignature),
                    Builders<Txn>.Update.Set(t => t.Input, txn.Input),
                    Builders<Txn>.Update.Set(t => t.Value, txn.Value),
                    Builders<Txn>.Update.Set(t => t.MaxFeePerGas, txn.MaxFeePerGas),
                    Builders<Txn>.Update.Set(t => t.MaxPriorityFeePerGas, txn.MaxPriorityFeePerGas),
                    Builders<Txn>.Update.Set(t => t.GasPrice, txn.GasPrice),
                    Builders<Txn>.Update.Set(t => t.Gas, txn.Gas),
                    Builders<Txn>.Update.Set(t => t.Nonce, txn.Nonce),
                    Builders<Txn>.Update.Set(t => t.IsApprove, txn.IsApprove),
                    Builders<Txn>.Update.Set(t => t.GotTokenInfo, txn.GotTokenInfo),
                    Builders<Txn>.Update.Set(t => t.Receipt, txn.Receipt),
                    Builders<Txn>.Update.Set(t => t.Notified, txn.Notified)
                };

                var updateDefinition = Builders<Txn>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<Txn>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public async Task<TxnInfosResult[]> GetTxnInfosCountAsync(string blockNumber)
        {
            var matchDocument = new BsonDocument {
                { "$match", new BsonDocument{
                    { "blockNumber", blockNumber},
                    { "receipt", new BsonDocument{ { "$ne", BsonNull .Value} } }
                } }
            };

            var projectDocument = new BsonDocument {
                { "$project", new BsonDocument{
                    { "_id", "$_id"},
                    { "succeeded", "$receipt.succeeded" },
                    { "gas", "$receipt.effectiveGasPrice"}
                } }
            };

            var pipelineDocument = new BsonDocument[] {
                matchDocument,
                projectDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<TxnInfosResult>(pipelineDocument, options).ToListAsync();
            return result.ToArray();
        }

        public async Task GetEstimatedGasFeesAsync(string blockNumber)
        {
            var txns = await GetManyAsync(p => p.Receipt != null && p.Receipt.Succeeded && p.BlockNumber == blockNumber);

            //txns.Select(_ => _.Receipt.GasUsed);
        }

        public async Task<List<Txn>> GetNotifiableTxnsAsync()
        {
            var specificStrings = new List<string> { 
                "atInversebrah",
                "removeLiquidity",
                "renounceOwnership",
                "_renounceOwnership"
            };

            var startWithFilter = Builders<Txn>.Filter.Regex("functionName", new BsonRegularExpression("^(?i)" + string.Join("|", specificStrings)));

            var orFilter = Builders<Txn>.Filter.Or(new List<FilterDefinition<Txn>> {
                startWithFilter,
                Builders<Txn>.Filter.Eq(_ => _.FunctionName, "atInversebrah"),
                Builders<Txn>.Filter.Eq(_ => _.FunctionName, "setfee"),
                Builders<Txn>.Filter.Eq(_ => _.FunctionName, "setFee"),
                Builders<Txn>.Filter.Eq(_ => _.FunctionName, "setfees"),
                Builders<Txn>.Filter.Eq(_ => _.FunctionName, "setFees"),
            });

            var andFilter = Builders<Txn>.Filter.And(
                new List<FilterDefinition<Txn>> {
                orFilter,
                Builders<Txn>.Filter.Ne(_ => _.Notified, true),
                Builders<Txn>.Filter.Ne(_ => _.Receipt, null),
                Builders<Txn>.Filter.Eq(_ => _.Receipt.Succeeded, true)
            });

            var result = await _collection.Find(andFilter).ToListAsync();
            return result;
        }

        public async Task BulkSetNotifiedAsync(IEnumerable<Txn> txns) {
            var updates = new List<WriteModel<Txn>>();

            var now = DateTime.UtcNow;

            foreach (var txn in txns)
            {
                txn.LastUpdatedDate = now;
                txn.UpdatedDates.Add(now);

                var filterDefinition = Builders<Txn>.Filter.Eq(t => t.Hash, txn.Hash);

                var updateDefs = new List<UpdateDefinition<Txn>>
                {
                    Builders<Txn>.Update.Set(t => t.Notified, true)
                };

                var updateDefinition = Builders<Txn>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<Txn>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public TxnsManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, TxnsManager> GetImplementationFactory()
        {
            static TxnsManager implementationFactory(IServiceProvider provider)
            {
                return new TxnsManager(provider.GetService<IOptions<MongoDbOptions>>(), "transactions");
            }

            return implementationFactory;
        }
    }

    public class BlockTxnsResult
    {
        [BsonId]
        public string Id { get; set; }
        public Txn[] Txns { get; set; }
    }

    public class TokenTxnsResult
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Owner { get; set; }
        public int Count { get; set; }
    }

    public class TxnInfosResult
    {
        [BsonId]
        public string Id { get; set; }
        public bool Succeeded { get; set; }
        public string Gas { get; set; }
    }
}
