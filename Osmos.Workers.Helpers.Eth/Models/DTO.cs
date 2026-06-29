using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;

namespace Osmos.Workers.Helpers.Eth.Models
{
    public partial class ApproveFunction : ApproveFunctionBase { }

    [Function("approve", "bool")]
    public class ApproveFunctionBase : FunctionMessage
    {
        [Parameter("address", "spender", 1)]
        public virtual string Spender { get; set; }
        [Parameter("uint256", "amount", 2)]
        public virtual BigInteger Amount { get; set; }
    }

    public partial class TransferFunction : TransferFunctionBase { }

    [Function("transfer", "bool")]
    public class TransferFunctionBase : FunctionMessage
    {
        [Parameter("address", "_to", 1)]
        public string To { get; set; }

        [Parameter("uint256", "_value", 2)]
        public BigInteger TokenAmount { get; set; }
    }

    public partial class GetPoolFunction : GetPoolFunctionBase { }
    [Function("getPool", "address")]
    public class GetPoolFunctionBase : FunctionMessage
    {
        [Parameter("address", "tokenA", 1)]
        public virtual string TokenA { get; set; }
        [Parameter("address", "tokenB", 2)]
        public virtual string TokenB { get; set; }
        [Parameter("uint24", "fee", 3)]
        public virtual BigInteger Fee { get; set; }
    }

    public class ReceiptsResult
    {
        public List<TransactionReceipt> receipts { get; set; }
    }

    public class Receipts
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public ReceiptsResult result { get; set; }
    }

    public class TransactionLog
    {
        public string TransactionHash { get; set; }
        public string Address { get; set; }
        public string BlockHash { get; set; }
        public string BlockNumber { get; set; }
        public string Data { get; set; }
        public string LogIndex { get; set; }
        public bool Removed { get; set; }
        public string[] Topics { get; set; }
        public string TransactionIndex { get; set; }
    }

    public class EthPrice {
        [JsonProperty("USD")]
        public double Usd { get; set; }
    }
}
