using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Osmos.Core.Data
{
    public class QueryOptions<TEntity>
        where TEntity : Entity
    {
        public QuerySortOptions<TEntity> SortOptions { get; set; }
        public QueryPaginationOptions Pagination { get; set; }
    }

    public class QuerySortOptions<TEntity>
            where TEntity : Entity
    {
        public Expression<Func<TEntity, object>> Field { get; set; }
        public bool Descending { get; set; }
    }
}
