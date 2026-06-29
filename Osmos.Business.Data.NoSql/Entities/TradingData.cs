using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class BaseToken
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

    public class H1
    {
        public int Buys { get; set; }
        public int Sells { get; set; }
    }

    public class H24
    {
        public int Buys { get; set; }
        public int Sells { get; set; }
    }

    public class H6
    {
        public int Buys { get; set; }
        public int Sells { get; set; }
    }

    public class Liquidity
    {
        public double Usd { get; set; }
        public double Base { get; set; }
        public double Quote { get; set; }
    }

    public class M5
    {
        public int Buys { get; set; }
        public int Sells { get; set; }
    }

    public class PairDataList {
        public Pair[] Pairs { get; set; }
    }

    public class Pair
    {
        public string ChainId { get; set; }
        public string DexId { get; set; }
        public string Url { get; set; }
        public string PairAddress { get; set; }
        public BaseToken BaseToken { get; set; }
        public BaseToken QuoteToken { get; set; }
        public string PriceNative { get; set; }
        public string PriceUsd { get; set; }
        public Txns Txns { get; set; }
        public Volume Volume { get; set; }
        public PriceChange PriceChange { get; set; }
        public Liquidity Liquidity { get; set; }
        public double Fdv { get; set; }
        public long PairCreatedAt { get; set; }
    }

    public class Pair2
    {
        public string ChainId { get; set; }
        public string DexId { get; set; }
        public string Url { get; set; }
        public string PairAddress { get; set; }
        public BaseToken BaseToken { get; set; }
        public BaseToken QuoteToken { get; set; }
        public string PriceNative { get; set; }
        public string PriceUsd { get; set; }
        public Txns Txns { get; set; }
        public Volume Volume { get; set; }
        public PriceChange PriceChange { get; set; }
        public Liquidity Liquidity { get; set; }
        public double Fdv { get; set; }
        public long PairCreatedAt { get; set; }
    }

    public class PriceChange
    {
        public double H24 { get; set; }
        public double H6 { get; set; }
        public double H1 { get; set; }
        public double M5 { get; set; }
    }

    //public class QuoteToken
    //{
    //    public string Symbol { get; set; }
    //}

    public class TradingData
    {
        public string SchemaVersion { get; set; }
        public List<Pair> Pairs { get; set; }
        public Pair Pair { get; set; }
    }

    public class Txns
    {
        public H24 H24 { get; set; }
        public H6 H6 { get; set; }
        public H1 H1 { get; set; }
        public M5 M5 { get; set; }
    }

    public class Volume
    {
        public double H24 { get; set; }
        public double H6 { get; set; }
        public double H1 { get; set; }
        public double M5 { get; set; }
    }
}
