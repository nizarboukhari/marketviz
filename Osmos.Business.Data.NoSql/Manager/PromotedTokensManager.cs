using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class PromotedTokensManager : MongoDbEntitiesManager<PromotedToken>
    {
        public PromotedTokensManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, PromotedTokensManager> GetImplementationFactory()
        {
            static PromotedTokensManager implementationFactory(IServiceProvider provider)
            {
                return new PromotedTokensManager(provider.GetService<IOptions<MongoDbOptions>>(), "promoted-tokens");
            }

            return implementationFactory;
        }
    }
}
