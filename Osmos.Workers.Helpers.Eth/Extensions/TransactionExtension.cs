using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Workers.Helpers.Eth.Models;

namespace Osmos.Workers.Helpers.Eth.Extensions
{
    public static class TransactionExtension
    {
        public static Txn ToTxn(this Transaction transaction, TxnReceipt txnReceipt = null)
        {

            var txn = new Txn
            {
                BlockNumber = transaction.BlockNumber.ToString(),
                Hash = transaction.TransactionHash,
                From = transaction.From,
                To = transaction.To,
                FunctionName = transaction.IsTransactionForFunctionMessage<TransferFunction>() ? "Transfer" : null,
                FunctionSignature = transaction?.Input?.ToFunctionName(),
                Input = transaction.Input.ToString(),
                Value = transaction.Value.Value.ToString(),
                Gas = transaction.Gas.ToString(),
                Nonce = transaction.Nonce.ToString(),
                MaxFeePerGas = transaction.MaxFeePerGas?.ToString(),
                MaxPriorityFeePerGas = transaction.MaxPriorityFeePerGas?.ToString(),
                GasPrice = transaction.GasPrice?.ToString(),
                IsApprove = transaction.IsTransactionForFunctionMessage<ApproveFunction>(),
                Receipt = txnReceipt
            };

            if (txnReceipt != null) {
                txn.BlockNumber = txn.Receipt.BlockNumber;
            }

            return txn;
        }
    }
}
