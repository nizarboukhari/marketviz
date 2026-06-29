using Osmos.Core.Data;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class TxnNotification: Entity
    {
        public TxnNotificationType Type { get; set; }
        public string TxnName { get; set; }
        public string TxnHash { get; set; }
        public string MakerAddress { get; set; }
        public string TokenAddress { get; set; }
        public string TokenName { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenOwner { get; set; }
        public string TokenDisplayName
        {
            get
            {
                if (TokenName == null || TokenSymbol == null) return null;
                return $"{TokenName}({TokenSymbol})";
            }
        }
    }

    public enum TxnNotificationType
    {
        TokenCreation,
        RenouncedOwnership,
        RemovedLiquidity,
        SetFees
    }
}
