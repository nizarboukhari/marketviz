using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.Messenger
{
    internal class MessengerApp : BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { TelegramHotTokensMessagesAsync, TelegramExpiredScoreMessagesAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private async Task TelegramHotTokensMessages() {
            var hotTokens = await _hotTokensManager.GetManyAsync();

            await UpdateTelegramHotTokensMessageAsync(hotTokens);

            await CreateTelegramHottestTokenMessageAsync(hotTokens);
        }

        private async Task TelegramHotTokensMessagesAsync() {
            await ActionsHelper.LoopAsync(TelegramHotTokensMessages, 6000);
        }

        private async Task UpdateTelegramHotTokensMessageAsync(HotToken[] hotTokensResult)
        {
            await ActionsHelper.SmoothRunAsync(async () => {
                await _telegramManager.UpdateTelegramHotTokensMessageAsync(hotTokensResult, _hotTokenScoreMaxThreshold);
            });
        }

        private async Task CreateTelegramHottestTokenMessageAsync(HotToken[] hotTokensResult) {
            await ActionsHelper.SmoothRunAsync(async () => {
                await _telegramManager.CreateTelegramHottestTokenMessageAsync(hotTokensResult, _hotTokenScoreMaxThreshold, _hotTokenExpiryTime);
            });
        }

        private async Task TelegramExpiredScoreMessagesAsync() {
            await ActionsHelper.LoopAsync(async () => {
                await _telegramManager.TelegramExpiredScoreMessagesLoop(_hotTokenScoreMinThreshold, _hotTokenExpiryTime);
            }, 12000);
        }

        #region internals

        private int _hotTokenScoreMaxThreshold = 12;
        private int _hotTokenScoreMinThreshold = 6;
        private int _hotTokenExpiryTime = 6;

        private readonly TxnsManager _txnsManager = null;
        private readonly HotTokensManager _hotTokensManager = null;

        private readonly TelegramManager _telegramManager = null;

        public MessengerApp(
            TxnsManager txnsManager,
            HotTokensManager hotTokensManager,
            TelegramManager telegramManager)
        {
            _txnsManager = txnsManager;
            _hotTokensManager = hotTokensManager;

            _telegramManager = telegramManager;
        }

        #endregion
    }

    internal static class TokenPricesExtensions
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
                Max = max.RelativeMax ?? new TokenPrice { 
                    Date = DateTime.UtcNow,
                    Value = 0
                }
            };
        }
    }

    internal class TokenPriceDiff
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

    internal class MaxOpportunityResult
    {
        public TokenPrice Min { get; set; }
        public TokenPrice Max { get; set; }
    }
}
