using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class DataInfosManager : MongoDbEntitiesManager<DataInfo>
    {
        public DataInfosManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, DataInfosManager> GetImplementationFactory()
        {
            static DataInfosManager implementationFactory(IServiceProvider provider)
            {
                return new DataInfosManager(provider.GetService<IOptions<MongoDbOptions>>(), "data-infos");
            }

            return implementationFactory;
        }
    }
}
