using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    public abstract class UpdatedEntity : Entity
    {
        public DateTime LastUpdatedDate { get; set; }
        public List<DateTime> UpdatedDates { get; set; }

        public UpdatedEntity()
        {
            LastUpdatedDate = CreatedDate;
            UpdatedDates = new List<DateTime> {
                CreatedDate
            };
        }
    }
}
