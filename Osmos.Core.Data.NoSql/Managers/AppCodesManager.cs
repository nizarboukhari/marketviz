using Microsoft.Extensions.Options;
using Osmos.Core.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Core.Data.NoSql.Managers
{
    public class AppCodesManager : MongoDbEntitiesManager<AppCode>
    {
        public async Task<AppCode> GetExistingAsync(AppCode appCode)
        {
            if (appCode == null) throw new ArgumentNullException();

            var appCodes = await GetManyAsync(c => c.AppId == appCode.AppId);

            var existing = appCodes.FirstOrDefault(c => c.Equals(appCode));
            return existing;
        }

        public async Task<bool> ExistsAsync(AppCode appCode)
        {
            var existing = await GetExistingAsync(appCode);
            return existing != null;
        }

        public override async Task<AppCode> CreateOneAsync(AppCode appCode)
        {
            if (appCode == null) throw new ArgumentNullException();

            var existing = await GetExistingAsync(appCode);
            if (existing != null) return existing;

            return await base.CreateOneAsync(appCode);
        }

        public static Func<IServiceProvider, AppCodesManager> GetImplementationFactory()
        {
            return GetImplementationFactory<AppCodesManager>("appCodes");
        }

        public AppCodesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }
    }
}
