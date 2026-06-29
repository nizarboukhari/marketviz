using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class Price
    {
        public double Rate { get; set; }
        public double Diff { get; set; }
        public double Diff7d { get; set; }
        public int Ts { get; set; }
        public double MarketCapUsd { get; set; }
        public double AvailableSupply { get; set; }
        public double Volume24h { get; set; }
        public double VolDiff1 { get; set; }
        public double VolDiff7 { get; set; }
        public double VolDiff30 { get; set; }
        public double Diff30d { get; set; }
        public double Bid { get; set; }
        public string Currency { get; set; }
    }

    public class TokenInfo
    {
        public string Address { get; set; }
        public string Decimals { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Symbol { get; set; }
        public string TotalSupply { get; set; }
        public int TransfersCount { get; set; }
        public int TxsCount { get; set; }
        public int LastUpdated { get; set; }
        public int IssuancesCount { get; set; }
        public int HoldersCount { get; set; }
        public string Website { get; set; }
        public string Image { get; set; }
        public int EthTransfersCount { get; set; }
        public int CountOps { get; set; }
        public Price Price { get; set; }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
        }
    }

    public class Holder
    {
        public string Address { get; set; }
        public object Balance { get; set; }
        public double Share { get; set; }

    }

    public class HolderList
    {
        public List<Holder> Holders { get; set; }
    }
}
