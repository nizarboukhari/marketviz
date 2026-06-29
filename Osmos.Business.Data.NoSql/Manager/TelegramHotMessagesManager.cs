using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class TelegramHotMessagesManager : MongoDbEntitiesManager<TelegramHotMessage>
    {
        public TelegramHotMessagesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, TelegramHotMessagesManager> GetImplementationFactory()
        {
            static TelegramHotMessagesManager implementationFactory(IServiceProvider provider)
            {
                return new TelegramHotMessagesManager(provider.GetService<IOptions<MongoDbOptions>>(), "telegram-hot-messages");
            }

            return implementationFactory;
        }
    }
}

