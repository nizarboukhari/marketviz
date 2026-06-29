using Osmos.Business.Data.NoSql.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Models
{
    public class TxnEx : Txn
    {
        public string TokenAddress { get; set; }
        public bool FromOwner { get; set; }
        public TxnExType Type { get; set; } = TxnExType.None;

        public TxnEx()
        {

        }

        public TxnEx(Txn txn, string tokenAddress = null)
        {
            Id = txn.Id;
            CreatedDate = txn.CreatedDate;
            LastUpdatedDate = txn.LastUpdatedDate;
            UpdatedDates = txn.UpdatedDates;
            BlockNumber = txn.BlockNumber;
            Hash = txn.Hash;
            From = txn.From;
            To = txn.To;
            FunctionName = txn.FunctionName;
            FunctionSignature = txn.FunctionSignature;
            Input = txn.Input;
            Value = txn.Value;
            MaxFeePerGas = txn.MaxFeePerGas;
            MaxPriorityFeePerGas = txn.MaxPriorityFeePerGas;
            GasPrice = txn.GasPrice;
            Gas = txn.Gas;
            Nonce = txn.Nonce;
            IsApprove = txn.IsApprove;
            GotTokenInfo = txn.GotTokenInfo;
            Receipt = txn.Receipt;

            TokenAddress = tokenAddress;
        }
    }

    public enum TxnExType
    {
        None = 0,
        Sell = -1,
        Buy = +1
    }
}
