using Osmos.Core.Data;
using System;
using System.Collections.Generic;

namespace Osmos.Business.Data.NoSql.Entities
{
    // remark: when changing this must change the Bulk Update in the manager
    public class HotToken: Entity
    {
        public double Score { get; set; }
        public Token Token { get; set; }
        public DateTime UpdatedDate { get; set; }

        public List<ScoreDate> ScoreDates { get; set; } = new List<ScoreDate>();

        public HotToken()
        {
            UpdatedDate = DateTime.UtcNow;
        }
    }

    public class ScoreDate {
        public DateTime Date { get; set; }
        public double Score { get; set; }
    }
}
