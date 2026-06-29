using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Worker.Models;
using Osmos.Business.Worker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Osmos.Business.Worker
{
    public static class TransactionExtension {
        public static RawTransaction ToRaw(this Transaction transaction) {
            return new RawTransaction {
                S = transaction.S,
                BlockHash = transaction.BlockHash,
                BlockNumber = transaction.BlockNumber?.ToString(),
                From = transaction.From,
                Gas = transaction.Gas?.ToString(),
                GasPrice = transaction.GasPrice?.ToString(),
                Input = transaction.Input,
                MaxFeePerGas = transaction.MaxFeePerGas?.ToString(),
                MaxPriorityFeePerGas = transaction.MaxPriorityFeePerGas?.ToString(),
                Nonce = transaction.Nonce?.ToString(),
                R = transaction.R,
                To = transaction.To,
                TransactionHash = transaction.TransactionHash,
                TransactionIndex = transaction.TransactionIndex?.ToString(),
                Type = transaction.Type?.ToString(),
                V = transaction.V,
                Value = transaction.Value?.ToString()
            };
        }

        public static Transaction ToProcessed(this RawTransaction transaction) {
            return new Transaction {
                S = transaction.S,
                BlockHash = transaction.BlockHash,
                BlockNumber = transaction.BlockNumber == null ? null : new HexBigInteger(transaction.BlockNumber),
                From = transaction.From,
                Gas = transaction.Gas == null ? null : new HexBigInteger(transaction.Gas),
                GasPrice = transaction.GasPrice == null ? null : new HexBigInteger(transaction.GasPrice),
                Input = transaction.Input,
                MaxFeePerGas = transaction.MaxFeePerGas == null ? null : new HexBigInteger(transaction.MaxFeePerGas),
                MaxPriorityFeePerGas = transaction.MaxPriorityFeePerGas == null ? null : new HexBigInteger(transaction.MaxPriorityFeePerGas),
                Nonce = transaction.Nonce == null ? null : new HexBigInteger(transaction.Nonce),
                R = transaction.R,
                To = transaction.To,
                TransactionHash = transaction.TransactionHash,
                TransactionIndex = transaction.TransactionIndex == null ? null : new HexBigInteger(transaction.TransactionIndex),
                Type = transaction.Type == null ? null : new HexBigInteger(transaction.Type),
                V = transaction.V,
                Value = transaction.Value == null ? null : new HexBigInteger(transaction.Value)
            };
        }

        public static Txn ToTxn(this Transaction transaction, TxnReceipt txnReceipt = null) { 
            
            var txn = new Txn
            {
                BlockNumber = transaction.BlockNumber.ToString(),
                Hash = transaction.TransactionHash,
                From = transaction.From,
                To = transaction.To,
                FunctionName = transaction.IsTransactionForFunctionMessage<TransferFunction>() ? "Transfer" : null,
                FunctionSignature = transaction.Input?.ToFunctionName(),
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

            return txn;
        }
    }

    public static class EnumerableExtension{
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }
    }

    public static class TokenPricesExtensions
    {
        public static MaxOpportunityResult ToMaxOpportunity(this IEnumerable<TokenPrice> tokenPrices)
        {
            if (tokenPrices.Count() < 2) return null;

            var differences = tokenPrices.Select(tp => new TokenPriceDiff { Point = tp }).ToArray();
            for (int i = 0; i < tokenPrices.Count() - 1; i++)
            {
                var maxPrice = tokenPrices.Skip(i + 1).Where(tp => tp.Value != null).OrderByDescending(tp => tp.Value).First();
                differences[i].RelativeMax = maxPrice;
            }

            var max = differences.OrderBy(_ => _.Diff).Last();

            return new MaxOpportunityResult
            {
                Min = max.Point,
                Max = max.RelativeMax
            };
        }

    }

    class TokenPriceDiff
    {
        public TokenPrice Point { get; set; }
        public TokenPrice RelativeMax { get; set; }
        public double Diff
        {
            get
            {
                if (Point.Value == null) return 0;
                if (RelativeMax == null || RelativeMax.Value == null) return 0;
                return (double)(RelativeMax.Value - Point.Value);
            }
        }
    }
    public class MaxOpportunityResult
    {
        public TokenPrice Min { get; set; }
        public TokenPrice Max { get; set; }
    }

}
