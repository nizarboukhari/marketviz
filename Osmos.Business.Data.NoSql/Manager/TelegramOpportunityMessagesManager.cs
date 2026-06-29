using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Threading.Tasks;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class TelegramOpportunityMessagesManager : MongoDbEntitiesManager<TelegramOpportunityMessage>
    {
        public async Task<OpportunityResult[]> GetTopOpportunityAsync()
        {

            var now = DateTime.UtcNow;
            var time = now.AddDays(-1);

            var minValueNullCondition = new BsonDocument { { "minPrice.value", new BsonDocument { { "$ne", BsonNull.Value } } } };
            var minValueZeroCondition = new BsonDocument { { "minPrice.value", new BsonDocument { { "$ne", 0 } } } };
            var minDateCondition = new BsonDocument { { "minPrice.date", new BsonDocument { { "$ne", BsonNull.Value } } } };
            var maxValueNullCondition = new BsonDocument { { "maxPrice.value", new BsonDocument { { "$ne", BsonNull.Value } } } };
            var maxDateCondition = new BsonDocument { { "maxPrice.date", new BsonDocument { { "$ne", BsonNull.Value } } } };
            var dateCondition = new BsonDocument { { "createdDate", new BsonDocument { { "$gte", time } } } };
            var andCondition = new BsonDocument { { "$and", new BsonArray {
                minValueNullCondition,
                minValueZeroCondition,
                minDateCondition,
                maxValueNullCondition,
                maxDateCondition,
                dateCondition,
            } } };

            var match0Document = new BsonDocument {
                {"$match",  andCondition}
            };

            var sortDateDocument = new BsonDocument {
                { "$sort", new BsonDocument{ { "createdDate", -1} } }
            };

            var projectDocument = new BsonDocument {
                {"$project", new BsonDocument{
                    { "_id", "$_id"},
                    { "tokenAddress", "$tokenAddress"},
                    { "minPrice", "$minPrice"},
                    { "maxPrice", "$maxPrice"},
                    { "percentage", new BsonDocument{ {
                                    "$divide", new BsonArray{
                                            "$maxPrice.value", "$minPrice.value"
                                        }
                                    }}
                    }
                } }
            };

            var match1Document = new BsonDocument { { "$match", new BsonDocument { { "percentage", new BsonDocument { { "$lte", 100000 } } } } } };

            var sortPercentageDocument = new BsonDocument {
                { "$sort", new BsonDocument{ { "percentage", -1} } }
            };

            var limitDocument = new BsonDocument { { "$limit", 36 } };

            var pipelineDocument = new BsonDocument[] {
                match0Document,
                sortDateDocument,
                projectDocument,
                match1Document,
                sortPercentageDocument,
                limitDocument
            };

            var options = new AggregateOptions
            {
                AllowDiskUse = true
            };

            var result = await _collection.Aggregate<OpportunityResult>(pipelineDocument, options).ToListAsync();
            return result.ToArray();
        }

        public TelegramOpportunityMessagesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, TelegramOpportunityMessagesManager> GetImplementationFactory()
        {
            static TelegramOpportunityMessagesManager implementationFactory(IServiceProvider provider)
            {
                return new TelegramOpportunityMessagesManager(provider.GetService<IOptions<MongoDbOptions>>(), "telegram-opportunity-messages");
            }

            return implementationFactory;
        }
    }

    public class OpportunityResult
    {
        [BsonId]
        public string Id { get; set; }
        public string TokenAddress { get; set; }
        public double Percentage { get; set; }
        public TokenPrice MinPrice { get; set; }
        public TokenPrice MaxPrice { get; set; }
    }
}

