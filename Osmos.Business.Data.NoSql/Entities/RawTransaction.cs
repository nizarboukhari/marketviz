using Nethereum.RPC.Eth.DTOs;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class RawTransaction : Entity
    {
        public string S { get; set; }
        public string R { get; set; }
        public string Nonce { get; set; }
        public string Input { get; set; }
        public string Value { get; set; }
        public string MaxPriorityFeePerGas { get; set; }
        public string MaxFeePerGas { get; set; }
        public string V { get; set; }
        public string GasPrice { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string BlockNumber { get; set; }
        public string BlockHash { get; set; }
        public string Type { get; set; }
        public string TransactionIndex { get; set; }
        public string TransactionHash { get; set; }
        public string Gas { get; set; }
        public bool Processed { get; set; }
        public TransactionReceipt Receipt { get; set; }
    }
}
