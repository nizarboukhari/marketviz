using System;

namespace Osmos.Business.Data.NoSql.Entities
{
    // remark: when changing this must change the Bulk Update in the manager
    public class Txn : UpdatedEntity
    {
        public string BlockNumber { get; set; }
        public string Hash { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string FunctionName { get; set; }
        public string FunctionSignature { get; set; }

        public string Input { get; set; }

        public string Value { get; set; }

        public string MaxFeePerGas { get; set; }

        public string MaxPriorityFeePerGas { get; set; }

        public string GasPrice { get; set; }

        public string Gas { get; set; }

        public string Nonce { get; set; }

        public bool IsApprove { get; set; }
        [Obsolete]
        public bool GotTokenInfo { get; set; }
        public bool ApprovalDone { get; set; }

        public bool Notified { get; set; }

        public TxnReceipt Receipt { get; set; }
    }

    public class TxnReceipt {
        public string Type { get; set; }
        public bool Succeeded { get; set; }
        public string EffectiveGasPrice { get; set; }
        public string GasUsed { get; set; }
        public string CumulativeGasUsed { get; set; }
        public string BlockNumber { get; set; }
        public string BlockHash { get; set; }
        public string[] Logs { get; set; }
        public string LogsBloom { get; set; }
        public string TransactionIndex { get; set; }
        public string TransactionHash { get; set; }
        public string To { get; set; }
        public string Status { get; set; }
        public string ContractAddress { get; set; }
        public string Root { get; set; }
        public string From { get; set; }
    }
}
