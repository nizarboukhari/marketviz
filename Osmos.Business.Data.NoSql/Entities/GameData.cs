using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class GameData : Entity
    {
        public DateTime Date { get; set; }
        public int TxnsCount { get; set; }
        public string GasUsed { get; set; }
        public string GasLimit { get; set; }
        public string Size { get; set; }
        public string TotalDifficulty { get; set; }
        public string Difficulty { get; set; }
        public string BaseFeePerGas { get; set; }
        public decimal GasFees { get; set; }
        public double ComputeTime { get; set; }
        public double EthPrice { get; set; }
        public HotTokenItem[] HotTokens { get; set; }

    }

    public class BlockData
    {
        public string Number { get; set; }
        public int TxnsCount { get; set; }
        public DateTime Date { get; set; }
        public string GasUsed { get; set; }
        public string GasLimit { get; set; }
        public string Size { get; set; }
        public string TotalDifficulty { get; set; }
        public string Difficulty { get; set; }
        public string BaseFeePerGas { get; set; }
    }

    public class HotTokenItem
    {
        public int Rank { get; set; }
        public string Address { get; set; }
        public double Score { get; set; }
        public string PriceNative { get; set; }
        public string PriceUsd { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string DisplayName { get; set; }
        public bool RenouncedOwnership { get; set; }
        public bool SourceCodeExists { get; set; }
    }
}
