using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class TokensManager : MongoDbEntitiesManager<Token>
    {
        //public async Task<Token> UpdateOneExAsync(Token token, bool approve = false)
        //{
        //    var now = DateTime.UtcNow;
        //    token.LastUpdatedDate = now;
        //    token.UpdatedDates.Add(now);

        //    if (approve)
        //    {
        //        token.LastApprovedDate = now;
        //        token.ApprovedDates.Add(now);
        //    }

        //    return await UpdateOneAsync(token);
        //}

        public async Task<TokenScore[]> GetHotTokensAsync(int minutes = 12, int limit = 12)
        {

            var now = DateTime.UtcNow;
            var time = now.AddMinutes(-minutes);

            var lastApprovedCondition = new BsonDocument { { "lastApprovedDate", new BsonDocument { { "$gte", time } } } };
            var pairDataCondition = new BsonDocument { { "pairs", new BsonDocument { {"$type", "array"}, { "$ne", new BsonArray { } } } } };
            //var andCondition = new BsonDocument { { "$and", new BsonArray { lastApprovedCondition, pairDataCondition } } };
            var andCondition = new BsonDocument { { "$and", new BsonArray { lastApprovedCondition } } };

            var matchDocument = new BsonDocument {
                {"$match",  andCondition}
            };

            var projectDocument = new BsonDocument {
                {"$project", new BsonDocument{
                    { "_id", "$address"},
                    { "token", "$$ROOT"},
                    { "score", new BsonDocument{ {
                                    "$size", new BsonDocument{ {
                                            //"$filter", new BsonDocument{{ "input", "$approvedDates"}, {"cond", new BsonDocument { { "$gte", new BsonArray { "$$this", time } } } }}
                                            "$filter", new BsonDocument{{ "input", "$approvedDates"}, {"cond", new BsonDocument { { "$gte", new BsonArray { "$$this.date", time } } } }}
                                        } }
                                    }}
                    }
                } }
            };

            var sortDocument = new BsonDocument {
                { "$sort", new BsonDocument{ { "score", -1} } }
            };

            var limitDocument = new BsonDocument { { "$limit", limit } };

            var pipelineDocument = new BsonDocument[] {
                matchDocument,
                projectDocument,
                sortDocument,
                limitDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<TokenScore>(pipelineDocument, options).ToListAsync();
            return result.ToArray();
        }

        public async Task<List<TokenScore>> GetTokenScoresAsync(string[] addresses) {
            var now = DateTime.UtcNow;
            var time = now.AddMinutes(-12);

            var projectDocument = new BsonDocument {
                {"$project", new BsonDocument{
                    { "_id", "$address"},
                    { "token", "$$ROOT"},
                    { "score", new BsonDocument{ {
                                    "$size", new BsonDocument{ {
                                            "$filter", new BsonDocument{{ "input", "$approvedDates"}, {"cond", new BsonDocument { { "$gte", new BsonArray { "$$this.date", time } } } }}
                                        } }
                                    }}
                    }
                } }
            };

            var addressesCondition = new BsonDocument { { "address", new BsonDocument { { "$in", new BsonArray(addresses) } } } };

            var matchDocument = new BsonDocument {
                {"$match",  addressesCondition}
            };

            var pipelineDocument = new BsonDocument[] {
                matchDocument,
                projectDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<TokenScore>(pipelineDocument, options).ToListAsync();

            return result;
        }

        public async Task<double?> GetTokenScoreAsync(string address)
        {
            var now = DateTime.UtcNow;
            var time = now.AddMinutes(-12);

            var projectDocument = new BsonDocument {
                {"$project", new BsonDocument{
                    { "_id", "$address"},
                    //{ "token", "$$ROOT"},
                    { "score", new BsonDocument{ {
                                    "$size", new BsonDocument{ {
                                            "$filter", new BsonDocument{{ "input", "$approvedDates"}, {"cond", new BsonDocument { { "$gte", new BsonArray { "$$this.date", time } } } }}
                                        } }
                                    }}
                    }
                } }
            };

            var addressCondition = new BsonDocument { { "address", address } };

            var matchDocument = new BsonDocument {
                {"$match",  addressCondition}
            };

            var pipelineDocument = new BsonDocument[] {
                matchDocument,
                projectDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<TokenScore>(pipelineDocument, options).FirstOrDefaultAsync();

            return result?.Score;
        }

        public async Task BlukUpdateManyAsync(IEnumerable<Token> tokens, bool approve = false) {

            var updates = new List<WriteModel<Token>>();

            var now = DateTime.UtcNow;

            if (!tokens.Any()) return;
            foreach (var token in tokens)
            {
                token.LastUpdatedDate = now;
                token.UpdatedDates.Add(now);

                if (approve && token.ApproveTxns != null && token.ApproveTxns.Any())
                {
                    var lastApprovedDate = token.ApproveTxns.OrderBy(_ => _.CreatedDate).Last().CreatedDate;
                    if (lastApprovedDate > token.LastApprovedDate) token.LastApprovedDate = lastApprovedDate;
                    token.ApprovedDates.AddRange(token.ApproveTxns.Select(_ => new ApprovalDate {
                        Date = _.CreatedDate,
                        TransactionHash = _.Hash
                    }));
                    token.ApprovedDates = token.ApprovedDates.OrderBy(_ => _.Date).ToList();
                    token.Approvals += token.ApproveTxns.Length;
                }

                //token.UpdatedDates = token.UpdatedDates.Where(_ => (now - _).TotalMinutes <= 30).ToList();
                token.UpdatedDates = token.UpdatedDates.Where(_ => (now - _).TotalMinutes <= 30).ToList();
                token.ApprovedDates = token.ApprovedDates.Where(_ => (now - _.Date).TotalMinutes <= 30).ToList();
                token.Prices = token.Prices.Where(_ => (now - _.Date).TotalMinutes <= 30).ToList();

                var filterDefinition = Builders<Token>.Filter.Eq(t => t.Address, token.Address);

                var updateDefs = new List<UpdateDefinition<Token>>
                {
                    Builders<Token>.Update.Set(t => t.Name, token.Name),
                    Builders<Token>.Update.Set(t => t.Symbol, token.Symbol),
                    Builders<Token>.Update.Set(t => t.Owner, token.Owner),
                    //Builders<Token>.Update.Set(t => t.PairAddress, token.PairAddress),
                    //Builders<Token>.Update.Set(t => t.PairTradingData, token.PairTradingData),
                    Builders<Token>.Update.Set(t => t.TokenInfo, token.TokenInfo),
                    Builders<Token>.Update.Set(t => t.Top100Holders, token.Top100Holders),
                    Builders<Token>.Update.Set(t => t.SourceCode, token.SourceCode),
                    Builders<Token>.Update.Set(t => t.LastInfoDate, token.LastInfoDate)
                };

                if (approve) {
                    updateDefs.AddRange(new UpdateDefinition<Token>[]{
                        Builders<Token>.Update.Set(t => t.Approvals, token.Approvals),
                        Builders<Token>.Update.Set(t => t.LastApprovedDate, token.LastApprovedDate),
                        Builders<Token>.Update.Set(t => t.ApprovedDates, token.ApprovedDates)
                    });
                }

                if (!approve) {
                    updateDefs.AddRange(new UpdateDefinition<Token>[]{
                        Builders<Token>.Update.Set(t => t.Pairs, token.Pairs),
                        Builders<Token>.Update.Set(t => t.Prices, token.Prices)
                    });
                }

                var updateDefinition = Builders<Token>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<Token>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public async Task BlukUpdateManySecurityAsync(IEnumerable<Token> tokens)
        {

            var updates = new List<WriteModel<Token>>();

            if (!tokens.Any()) return;
            foreach (var token in tokens)
            {
                var filterDefinition = Builders<Token>.Filter.Eq(t => t.Address, token.Address);

                var updateDefs = new List<UpdateDefinition<Token>>
                {
                    Builders<Token>.Update.Set(t => t.Security, token.Security)
                };

                var updateDefinition = Builders<Token>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<Token>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public TokensManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, TokensManager> GetImplementationFactory()
        {
            static TokensManager implementationFactory(IServiceProvider provider)
            {
                return new TokensManager(provider.GetService<IOptions<MongoDbOptions>>(), "tokens");
            }

            return implementationFactory;
        }
    }

    public class TokenScore {
        [BsonId]
        public string Id { get; set; }
        public double Score { get; set; }
        public Token Token { get; set; }
    }
}
