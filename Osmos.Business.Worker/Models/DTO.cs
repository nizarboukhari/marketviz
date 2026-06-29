using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

namespace Osmos.Business.Worker.Models
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
    public partial class PairCreatedEventDTO : PairCreatedEventDTOBase { }

    [Event("PairCreated")]
    public class PairCreatedEventDTOBase : IEventDTO
    {
        [Parameter("address", "token0", 1, true)]
        public virtual string Token0 { get; set; }
        [Parameter("address", "token1", 2, true)]
        public virtual string Token1 { get; set; }
        [Parameter("address", "pair", 3, false)]
        public virtual string Pair { get; set; }
        [Parameter("uint256", "", 4, false)]
        public virtual BigInteger ReturnValue4 { get; set; }
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
}
