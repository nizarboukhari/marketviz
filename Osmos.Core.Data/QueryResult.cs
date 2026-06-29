using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Core.Data
{
    public class QueryPaginationOptions
    {
        public int? Size { get; set; }
        public int? Page { get; set; }
        public decimal? PageCount { get; set; }
    }

    public class QueryResult<TEntity>
        where TEntity : Entity
    {
        public TEntity[] Entities { get; set; }
        public long Total { get; set; }
        public QueryPaginationOptions Pagination { get; set; }
    }
}
