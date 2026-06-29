using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Workers.Notifier
{
    internal class NotifierApp: BaseApp
    {
        protected override async Task RunAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://mrktviz-api.azurewebsites.net/appHub")
                //.WithUrl("http://localhost:5000/appHub")
                .Build();

            //_connection.Closed += async (error) =>
            //{
            //    Console.WriteLine("rt connection is down, reconnecting ..");
            //    await Task.Delay(new Random().Next(0, 5) * 1000);
            //    await _connection.StartAsync();
            //};

            _connection.On("Pong", () => {
                Console.WriteLine("Pong");
            });

            await _connection.StartAsync();

            var functions = new Func<Task>[] { NotifierLoop };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        public async Task NotifierAsync() {

            var now = DateTime.UtcNow;
            
            await _connection.SmoothInvokeAsync("Ping");

            var txns = await _txnsManager.GetNotifiableTxnsAsync();

            Console.WriteLine($"got {txns.Count} txns");

            foreach (var txn in txns)
            {
                TxnNotificationType txnNotificationType;

                string tokenAddress = null;
                //BigInteger? coinsRemoved = null;
                //BigInteger? wethRemoved = null;
                if (txn.FunctionName.ToLower().StartsWith("removeliquidity"))
                {
                    if (txn.Receipt == null || txn.Receipt.Logs == null || txn.Receipt.Logs.Length < 2) continue;

                    var logs = txn.Receipt.Logs
                            .Select(_ => JsonConvert.DeserializeObject<TransactionLog>(_))
                            .ToArray();

                    tokenAddress = logs[^2].Address;

                    //try
                    //{
                    //    coinsRemoved = BigInteger.Parse(logs[^2].Data, NumberStyles.HexNumber);
                    //    wethRemoved = BigInteger.Parse(logs[^1].Data, NumberStyles.HexNumber);
                    //}
                    //catch (Exception e)
                    //{
                    //    ;
                    //}
                }
                else if (txn.FunctionName.ToLower() == "atInversebrah".ToLower())
                {
                    if (txn.Receipt == null) continue;

                    tokenAddress = txn.Receipt.ContractAddress;
                }
                else if (txn.To != null)
                {
                    tokenAddress = txn.To;
                }
                else {
                    Console.WriteLine($"can not find token address from txn {txn.Hash}");
                    continue;
                }

                string name = null;
                string symbol = null;
                string owner = null;
                string displayName = null;
                var token = await _tokensManager.GetOneAsync(_ => _.Address == tokenAddress);
                if (token == null)
                {
                    var tokenInfos = await GetTokenInfosAsync(tokenAddress);
                    if (tokenInfos == null) {
                        Console.WriteLine($"can not find token {tokenAddress} for txn {txn.Hash}");
                        continue;
                    }

                    name = tokenInfos.Name;
                    symbol = tokenInfos.Symbol;
                    owner = tokenInfos.Owner;
                    displayName = tokenInfos.DisplayName;
                }
                else {
                    name = token.Name;
                    symbol = token.Symbol;
                    owner = token.Owner;
                    displayName = token.DisplayName;
                }

                if (name == null || symbol == null) {
                    continue;
                }

                if (name.ToLower().StartsWith("uniswap")) {
                    continue;
                }

                if (txn.FunctionName.ToLower().Contains("atInversebrah".ToLower()))
                {
                    txnNotificationType = TxnNotificationType.TokenCreation;
                }
                else if (txn.FunctionName.ToLower().Contains("removeLiquidity".ToLower()))
                {
                    txnNotificationType = TxnNotificationType.RemovedLiquidity;
                }
                else if (txn.FunctionName.ToLower().Contains("renounceOwnership".ToLower()))
                {
                    txnNotificationType = TxnNotificationType.RenouncedOwnership;
                }
                else {
                    txnNotificationType = TxnNotificationType.SetFees;
                }

                var txnNotification = new TxnNotification { 
                    Type = txnNotificationType,
                    TokenSymbol = symbol,
                    MakerAddress = txn.From,
                    TokenAddress = tokenAddress,
                    TokenName = name,
                    TokenOwner = owner,
                    TxnHash = txn.Hash,
                    TxnName = txn.FunctionName
                };

                bool succeeded = await _connection.SmoothInvokeAsync("NotifyTxn", txnNotification);
                if (!succeeded) {
                    Console.WriteLine($"missed notifying txn {txn.Hash} because signalR is {_connection.State}");
                    continue;
                }

                await _txnNotificationsManager.CreateOneAsync(txnNotification);
                Console.WriteLine($"notified {txn.FunctionName} from {txn.From} to {displayName}");
            }

            if (txns.Any()) {
                await _txnsManager.BulkSetNotifiedAsync(txns);
                Console.WriteLine($"notified about {txns.Count} txns in {(DateTime.UtcNow - now).TotalSeconds} seconds");
            }
        }

        public async Task NotifierLoop() {
            await ActionsHelper.LoopAsync(NotifierAsync, 6000);
        }

        #region internals

        private HubConnection _connection = null;
        private readonly TokensManager _tokensManager = null;
        private readonly TxnsManager _txnsManager = null;
        private readonly TxnNotificationsManager _txnNotificationsManager = null;
        private readonly EthService _ethService = null;

        public NotifierApp(TokensManager tokensManager, TxnsManager txnsManager, TxnNotificationsManager txnNotificationsManager, EthService ethService)
        {
            _tokensManager = tokensManager;
            _txnsManager = txnsManager;
            _txnNotificationsManager = txnNotificationsManager;
            _ethService = ethService;
        }

        private async Task<TokenInfosResult> GetTokenInfosAsync(string address) {

            try
            {
                string name = null;
                string symbol = null;
                string owner = null;
                TokenSourceCode sourceCode = null;

                if (sourceCode == null || sourceCode.MissingSourceCode() || sourceCode.ABI == _ethService.Settings.DefautltABI)
                {
                    sourceCode = await _ethService.GetSourceCodeSmoothAsync(address);
                }

                string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : _ethService.Settings.DefautltABI;
                var tokenContract = _ethService.GetContractWithABISmooth(address, abi);

                var tasks = new List<Task>();

                if (tokenContract != null)
                {
                    if (name == null)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            name = await _ethService.GetTokenNameAsync(tokenContract);
                        }));
                    }
                    if (symbol == null)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            symbol = await _ethService.GetTokenSymbolAsync(tokenContract);
                        }));
                    }

                    tasks.Add(Task.Run(async () =>
                    {
                        // renounce ownership logic here
                        owner = await _ethService.GetTokenOwnerAsync(tokenContract);
                    }));
                }

                await Task.WhenAll(tasks);

                return new TokenInfosResult
                {
                    Name = name,
                    Symbol = symbol,
                    Owner = owner
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"can not get token infos for {address} because {e.Message}");
                return null;
            }
        }

        #endregion

    }

    public class TokenInfosResult {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Owner { get; set; }
        public string DisplayName
        {
            get
            {
                if (Name == null || Symbol == null) return null;
                return $"{Name}({Symbol})";
            }
        }
    }
}
