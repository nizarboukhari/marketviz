using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class BetsManager : MongoDbEntitiesManager<Bet>
    {
        public BetsManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, BetsManager> GetImplementationFactory()
        {
            static BetsManager implementationFactory(IServiceProvider provider)
            {
                return new BetsManager(provider.GetService<IOptions<MongoDbOptions>>(), "bets");
            }

            return implementationFactory;
        }
    }
}
