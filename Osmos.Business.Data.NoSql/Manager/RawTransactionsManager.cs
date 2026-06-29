using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    [Obsolete]
    public class RawTransactionsManager : MongoDbEntitiesManager<RawTransaction>
    {
        public RawTransactionsManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, RawTransactionsManager> GetImplementationFactory()
        {
            static RawTransactionsManager implementationFactory(IServiceProvider provider)
            {
                return new RawTransactionsManager(provider.GetService<IOptions<MongoDbOptions>>(), "raw-transactions");
            }

            return implementationFactory;
        }
    }
}
