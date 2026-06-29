using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class GameDataManager : MongoDbEntitiesManager<GameData>
    {
        public GameDataManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, GameDataManager> GetImplementationFactory()
        {
            static GameDataManager implementationFactory(IServiceProvider provider)
            {
                return new GameDataManager(provider.GetService<IOptions<MongoDbOptions>>(), "game-data");
            }

            return implementationFactory;
        }
    }
}
