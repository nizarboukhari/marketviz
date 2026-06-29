using Osmos.Core.Data;
using System;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class DataInfo : Entity
    {
        public BlockInfos CurrentBlock { get; set; }
        public BlockInfos PreviousBlock { get; set; }
        public decimal GasFees { get; set; }

        public DateTime UpdatedDate { get; set; }
        public DataInfo()
        {
            UpdatedDate = DateTime.UtcNow;
        }
    }

    public class BlockInfos: BlockData
    {
        public int SucceededTxnsCount { get; set; }
        public int FailedTxnsCount { get; set; }
    }
}
