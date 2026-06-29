using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    [Obsolete]
    public class HolderInfosManager : MongoDbEntitiesManager<HolderInfo>
    {
        public HolderInfosManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, HolderInfosManager> GetImplementationFactory()
        {
            static HolderInfosManager implementationFactory(IServiceProvider provider)
            {
                return new HolderInfosManager(provider.GetService<IOptions<MongoDbOptions>>(), "holder-infos");
            }

            return implementationFactory;
        }
    }
}
