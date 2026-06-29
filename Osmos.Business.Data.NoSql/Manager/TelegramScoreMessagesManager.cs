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
using System.Threading.Tasks;

namespace Osmos.Business.Data.NoSql.Managers
{
    // remark: when changing this must change the Bulk Update in the manager
    public class TelegramScoreMessagesManager : MongoDbEntitiesManager<TelegramScoreMessage>
    {
        public async Task BlukUpdateManyAsync(IEnumerable<TelegramScoreMessage> messages)
        {

            var updates = new List<WriteModel<TelegramScoreMessage>>();

            var now = DateTime.UtcNow;

            foreach (var message in messages)
            {
                var filterDefinition = Builders<TelegramScoreMessage>.Filter.Eq(t => t.Id, message.Id);

                var updateDefs = new List<UpdateDefinition<TelegramScoreMessage>>
                {
                    Builders<TelegramScoreMessage>.Update.Set(m => m.MessagesInfos, message.MessagesInfos),
                    Builders<TelegramScoreMessage>.Update.Set(m => m.Text, message.Text),
                    Builders<TelegramScoreMessage>.Update.Set(m => m.TokenAddress, message.TokenAddress),
                    Builders<TelegramScoreMessage>.Update.Set(m => m.Score, message.Score),
                    Builders<TelegramScoreMessage>.Update.Set(m => m.Deleted, message.Deleted),
                    Builders<TelegramScoreMessage>.Update.Set(m => m.UpdatedDate, now)
                };

                var updateDefinition = Builders<TelegramScoreMessage>.Update.Combine(updateDefs);

                updates.Add(new UpdateOneModel<TelegramScoreMessage>(filterDefinition, updateDefinition));
            }

            var result = await _collection.BulkWriteAsync(updates);
        }

        public TelegramScoreMessagesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, TelegramScoreMessagesManager> GetImplementationFactory()
        {
            static TelegramScoreMessagesManager implementationFactory(IServiceProvider provider)
            {
                return new TelegramScoreMessagesManager(provider.GetService<IOptions<MongoDbOptions>>(), "telegram-score-messages");
            }

            return implementationFactory;
        }
    }
}

