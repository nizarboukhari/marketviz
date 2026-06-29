using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.Data.NoSql.Managers
{
    public class IgnoredTokensManager : MongoDbEntitiesManager<IgnoredToken>
    {
        public async Task<string> CreateOneAsync(string address) {

            var token = await GetOneAsync(t => t.Id == address);
            if (token != null) return token.Id;

            token = new IgnoredToken {
                Id = address
            };

            await CreateOneAsync(token);

            return token.Id;
        }

        public async Task<List<string>> GetAllAsync() {
            var tokens = await GetManyAsync();
            return tokens.Select(t => t.Id).ToList();
        }

        public IgnoredTokensManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public static Func<IServiceProvider, IgnoredTokensManager> GetImplementationFactory()
        {
            static IgnoredTokensManager implementationFactory(IServiceProvider provider)
            {
                return new IgnoredTokensManager(provider.GetService<IOptions<MongoDbOptions>>(), "ignored-tokens");
            }

            return implementationFactory;
        }
    }
}
