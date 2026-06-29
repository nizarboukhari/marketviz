using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class HolderInfo : UpdatedEntity
    {
        public string Address { get; set; }
        public Holding[] Holdings{ get; set; }
    }

    public class Holding
    {
        public string Address { get; set; }
        public decimal Value { get; set; }
    }
}
