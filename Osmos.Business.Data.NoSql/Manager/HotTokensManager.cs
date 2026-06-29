using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class HotTokensManager : MongoDbEntitiesManager<HotToken>
    {
        public async Task BlukUpdateManyAsync(IEnumerable<HotToken> hotTokens)
        {

            var updates = new List<WriteModel<HotToken>>();

            var now = DateTime.UtcNow;

            foreach (var item in hotTokens)
            {
                var filterDefinition = Builders<HotToken>.Filter.Eq(t => t.Id, item.Id);

                var updateDefs = new List<UpdateDefinition<HotToken>>
                {
                    Builders<HotToken>.Update.Set(m => m.Token, item.Token),
                    Builders<HotToken>.Update.Set(t => t.Score, item.Score),
                    Builders<HotToken>.Update.Set(t => t.ScoreDates, item.ScoreDates),
                    Builders<HotToken>.Update.Set(t => t.UpdatedDate, now)
                };

                var updateDefinition = Builders<HotToken>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<HotToken>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public HotTokensManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, HotTokensManager> GetImplementationFactory()
        {
            static HotTokensManager implementationFactory(IServiceProvider provider)
            {
                return new HotTokensManager(provider.GetService<IOptions<MongoDbOptions>>(), "hot-tokens");
            }

            return implementationFactory;
        }
    }
}

