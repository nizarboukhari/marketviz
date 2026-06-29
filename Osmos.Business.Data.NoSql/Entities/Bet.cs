using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class Bet : Entity
    {
        public string Wallet { get; set; }
        public string TargetBlock { get; set; }
        public string SourceBlock { get; set; }
        public string Item { get; set; }
        public bool Up { get; set; }
        public bool Pending { get; set; }
        public bool? SoloSuccess { get; set; }
        public bool? Success { get; set; }
        public HotTokenItem HotToken { get; set; }
        public List<string> GroupBetIds { get; set; } = new List<string> { };
    }
}
