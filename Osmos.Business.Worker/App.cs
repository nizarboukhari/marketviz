using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Services;
using Osmos.Business.Worker.Threads;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Osmos.Business.Worker
{
    internal class App
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalMilliseconds;
        }

        internal void Run()
        {
            _RunAsync().Wait();
        }

        private async Task _RunAsync()
        {

            //var token = await _tokensManager.GetOneAsync("5fc1aa1b-cc98-4d87-9e4a-97f31a322ff1");

            //var values = token.Prices.Select(tp => new object[] { ConvertToTimestamp(tp.Date), tp.Value * 1000000 }).ToArray();

            //var opportunity = token.Prices.ToMaxOpportunity();

            //string json = JsonConvert.SerializeObject(values);

            //var addresses = new string[] {
            //    "0x72d7b17bf63322a943d4a2873310a83dcdbc3c8d",
            //    "0xebbfad9cb89935ebe3809485b54a40d3a019176c",
            //    "0x994a95d454dcc3b52d8f1f3f7a7cf0b17b6c75f1",
            //    "0xf332553fc2ca4d7f84166cc9e2ae9b3562672665",
            //};

            //var result = await _tokensManager.GetTokenScoresAsync(addresses);

            //var botClient = new TelegramBotClient("6233741688:AAFsZYsED-vVshmgzfxWErmc81FVsuj-UnQ");
            //var chatId = new ChatId(-1001701981864); //dev

            //var message = await botClient.SendTextMessageAsync(chatId, "testing 123");

            //return;

            PendingTxnsThread.Start(_txnsManager);
            TxnsFunctionNamesThread.Start(_txnsManager);
            ReceiptsThread.Start(_txnsManager);
            //NewPairsThread.Start(_tokensManager);
            ApprovedTokensThread.Start(_tokensManager, _txnsManager, _ignoredTokensManager);
            UpdateHotTokensInfoThread.Start(_tokensManager, _txnsManager, _hotTokensManager, _telegramScoreMessagesManager, _telegramOpportunityMessagesManager);
            HotTokensThread.Start(_tokensManager, _hotTokensManager);

            do
            {
                Thread.Sleep(100);
            } while (true);
        }

        #region internals

        private readonly TokensManager _tokensManager = null;
        private readonly HolderInfosManager _holderInfosManager = null;
        private readonly TxnsManager _txnsManager = null;
        private readonly RawTransactionsManager _rawTransactionsManager = null;
        private readonly FunctionSignaturesTable _functionSignaturesTable = null;
        private readonly IgnoredTokensManager _ignoredTokensManager = null;
        private readonly TelegramScoreMessagesManager _telegramScoreMessagesManager = null;
        private readonly TelegramOpportunityMessagesManager _telegramOpportunityMessagesManager = null;
        private readonly HotTokensManager _hotTokensManager = null;

        public App(
            TokensManager tokensManager,
            HolderInfosManager holderInfosManager,
            TxnsManager txnsManager,
            RawTransactionsManager rawTransactionsManager,
            FunctionSignaturesTable functionSignaturesTable,
            IgnoredTokensManager ignoredTokensManager,
            TelegramScoreMessagesManager telegramScoreMessagesManager,
            TelegramOpportunityMessagesManager telegramOpportunityMessagesManager,
            HotTokensManager hotTokensManager)
        {
            _tokensManager = tokensManager;
            _holderInfosManager = holderInfosManager;
            _txnsManager = txnsManager;
            _rawTransactionsManager = rawTransactionsManager;

            _functionSignaturesTable = functionSignaturesTable;
            _ignoredTokensManager = ignoredTokensManager;

            _telegramScoreMessagesManager = telegramScoreMessagesManager;
            _telegramOpportunityMessagesManager = telegramOpportunityMessagesManager;
            _hotTokensManager = hotTokensManager;
        }

        #endregion
    }
}
