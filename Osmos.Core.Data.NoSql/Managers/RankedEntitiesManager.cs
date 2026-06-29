using Microsoft.Extensions.Options;
using Osmos.Core.Data.NoSql.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Osmos.Core.Data.NoSql.Managers
{
    public class RankedEntitiesManager<TEntity> : MongoDbEntitiesManager<TEntity>
        where TEntity : RankedEntity
    {
        public RankedEntitiesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }

        public async Task<TEntity[]> GetManyAsync()
        {
            var result = await GetManyAsync(new QueryOptions<TEntity>
            {
                SortOptions = new QuerySortOptions<TEntity>
                {
                    Field = e => e.Rank
                }
            }, e => true);

            return result.Entities;
        }
    }
}


