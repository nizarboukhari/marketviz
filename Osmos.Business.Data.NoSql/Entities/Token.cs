using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    // remark: when changing this must change the Bulk Update in the manager
    public class Token : UpdatedEntity
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string DisplayName
        {
            get
            {
                if (Name == null || Symbol == null) return null;
                return $"{Name}({Symbol})";
            }
        }
        //public decimal TotalSupply { get; set; }
        //public BigInteger Decimals { get; set; }
        public string Address { get; set; }
        [JsonIgnore]
        public int Approvals { get; set; }

        //public UnitConversion.EthUnit Unit { get; set; }
        //public object Unit { get; set; }
        //public string MainNetwork { get; set; }
        //public TokenSourceCode SourceCode { get; set; }
        //public bool IsInWatchlist { get; set; }
        public string Owner { get; set; }
        //public int HoldersCount { get; set; }
        public string PairAddress
        {
            get
            {
                return PairTradingData?.PairAddress;
            }
        }
        public Pair PairTradingData {
            get
            {
                if (Pairs == null || !Pairs.Any(_ => _.Liquidity != null)) return null;
                return Pairs.Where(_ => _.Liquidity != null).OrderByDescending(_ => _.Liquidity.Usd).FirstOrDefault();
            }
        }
        public Pair[] Pairs { get; set; } = new Pair[] { };
        public TokenInfo TokenInfo { get; set; }
        public List<Holder> Top100Holders { get; set; }
        public TokenSourceCode SourceCode { get; set; }
        [Obsolete]
        public DateTime? LastInfoDate { get; set; }
        [JsonIgnore]
        public DateTime? LastApprovedDate { get; set; }
        [JsonIgnore]
        public List<ApprovalDate> ApprovedDates { get; set; } = new List<ApprovalDate>();
        [JsonIgnore]
        public List<TokenPrice> Prices { get; set; } = new List<TokenPrice>();

        [BsonIgnore]
        [JsonIgnore]
        public Txn[] ApproveTxns { get; set; }
        public int ComputedScore
        {
            get
            {
                var now = DateTime.UtcNow;
                var time = now.AddMinutes(-12);

                return ApprovedDates.Where(_ => _.Date >= time).Count();
            }
        }

        public TokenSecurity Security { get; set; }
    }

    public class TokenPrice
    {
        public DateTime Date { get; set; }
        public double? Value { get; set; }

        public TokenPrice()
        {
            Date = DateTime.UtcNow;
        }
    }

    //ftmscan objects
    //TODO validate that everything is the same for etherscan and others
    public class SourceDetails
    {
        public string SourceCode { get; set; }
        public string ABI { get; set; }
        public string ContractName { get; set; }
        public string CompilerVersion { get; set; }
        public string OptimizationUsed { get; set; }
        public string Runs { get; set; }
        public string ConstructorArguments { get; set; }
        public string EVMVersion { get; set; }
        public string Library { get; set; }
        public string LicenseType { get; set; }
        public string Proxy { get; set; }
        public string Implementation { get; set; }
        public string SwarmSource { get; set; }
    }

    public class TokenSourceCode
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<SourceDetails> Result { get; set; }

        public bool Missing
        {
            get
            {
                return Result == null || !Result.Any() || Result[0].ABI.ToLower() == "contract source code not verified";
            }
        }

        public bool MissingSourceCode()
        {
            return Result == null || !Result.Any() || Result[0].ABI.ToLower() == "contract source code not verified";
        }

        public string ABI
        {
            get
            {
                return Result[0].ABI;
            }
        }
    }

    public class TokenSupply
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }
    }

    public class TokenBalance
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }

    }


    public class TokenPair
    {
        public string Token0 { get; set; }
        public string Token1 { get; set; }
        public string Address { get; set; }
    }

    public class ApprovalDate
    {
        public DateTime Date { get; set; }
        public string TransactionHash { get; set; }
    }

    public class TokenSecurity
    {
        [JsonProperty("anti_whale_modifiable")]
        public bool AntiWhaleModifiable { get; set; }

        [JsonProperty("buy_tax")]
        public double BuyTax { get; set; }

        [JsonProperty("can_take_back_ownership")]
        public bool CanTakeBackOwnership { get; set; }

        [JsonProperty("cannot_buy")]
        public bool CannotBuy { get; set; }

        [JsonProperty("cannot_sell_all")]
        public bool CannotSellAll { get; set; }

        [JsonProperty("dex")]
        public DexItem[] Dex { get; set; }

        [JsonProperty("external_call")]
        public bool ExternalCall { get; set; }

        [JsonProperty("hidden_owner")]
        public bool HiddenOwner { get; set; }

        [JsonProperty("holder_count")]
        public long HolderCount { get; set; }

        [JsonProperty("holders")]
        public HolderItem[] Holders { get; set; }

        [JsonProperty("honeypot_with_same_creator")]
        public bool HoneypotWithSameCreator { get; set; }

        [JsonProperty("is_anti_whale")]
        public bool IsAntiWhale { get; set; }

        [JsonProperty("is_blacklisted")]
        public bool IsBlacklisted { get; set; }

        [JsonProperty("is_honeypot")]
        public bool IsHoneypot { get; set; }

        [JsonProperty("is_in_dex")]
        public bool IsInDex { get; set; }

        [JsonProperty("is_mintable")]
        public bool IsMintable { get; set; }

        [JsonProperty("is_open_source")]
        public bool IsOpenSource { get; set; }

        [JsonProperty("is_proxy")]
        public bool IsProxy { get; set; }

        [JsonProperty("is_whitelisted")]
        public bool IsWhitelisted { get; set; }

        [JsonProperty("lp_holder_count")]
        public long LpHolderCount { get; set; }

        [JsonProperty("lp_holders")]
        public HolderItem[] LpHolders { get; set; }

        [JsonProperty("lp_total_supply")]
        public decimal LpTotalSupply { get; set; }

        [JsonProperty("owner_address")]
        public string OwnerAddress { get; set; }

        [JsonProperty("owner_balance")]
        public string OwnerBalance { get; set; }

        [JsonProperty("owner_change_balance")]
        public bool OwnerChangeBalance { get; set; }

        [JsonProperty("owner_percent")]
        public double OwnerPercent { get; set; }

        [JsonProperty("personal_slippage_modifiable")]
        public bool PersonalSlippageModifiable { get; set; }

        [JsonProperty("selfdestruct")]
        public bool Selfdestruct { get; set; }

        [JsonProperty("sell_tax")]
        public double SellTax { get; set; }

        [JsonProperty("slippage_modifiable")]
        public bool SlippageModifiable { get; set; }

        [JsonProperty("token_name")]
        public string TokenName { get; set; }

        [JsonProperty("token_symbol")]
        public string TokenSymbol { get; set; }

        [JsonProperty("total_supply")]
        public decimal TotalSupply { get; set; }

        [JsonProperty("trading_cooldown")]
        public bool TradingCooldown { get; set; }

        [JsonProperty("transfer_pausable")]
        public bool TransferPausable { get; set; }
    }

    public class DexItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("liquidity")]
        public decimal Liquidity { get; set; }

        [JsonProperty("pair")]
        public string Pair { get; set; }
    }

    public class HolderItem
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("is_contract")]
        public bool IsContract { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("percent")]
        public decimal Percent { get; set; }

        [JsonProperty("is_locked")]
        public bool IsLocked { get; set; }
    }
}
