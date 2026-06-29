using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class TxnNotificationsManager : MongoDbEntitiesManager<TxnNotification>
    {
        public TxnNotificationsManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, TxnNotificationsManager> GetImplementationFactory()
        {
            static TxnNotificationsManager implementationFactory(IServiceProvider provider)
            {
                return new TxnNotificationsManager(provider.GetService<IOptions<MongoDbOptions>>(), "txn-notifications");
            }

            return implementationFactory;
        }
    }
}
