using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Models;
using Osmos.Business.Worker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Osmos.Business.Worker.Threads
{
    public static class UpdateHotTokensInfoThread
    {
        public static void Start(
            TokensManager tokensManager, 
            TxnsManager txnsManager,
            HotTokensManager hotTokensManager,
            TelegramScoreMessagesManager telegramScoreMessagesManager, 
            TelegramOpportunityMessagesManager telegramOpportunityMessagesManager)
        {
            var updateHotTokensInfoThreadLoop = new Thread(new ThreadStart(() => UpdateHotTokensInfoLoop(tokensManager, txnsManager, hotTokensManager, telegramScoreMessagesManager)));
            var expiredMessagesLoop = new Thread(new ThreadStart(() => TelegramExpiredScoreMessagesLoop(tokensManager, telegramScoreMessagesManager, telegramOpportunityMessagesManager)));

            updateHotTokensInfoThreadLoop.Start();
            expiredMessagesLoop.Start();
        }

        private static Logger _logger = new Logger("UpdateHotTokensInfoThread", true);

        private static async void UpdateHotTokensInfoLoop(
            TokensManager tokensManager, 
            TxnsManager txnsManager,
            HotTokensManager hotTokensManager,
            TelegramScoreMessagesManager telegramScoreMessagesManager)
        {
            do
            {
                try
                {
                    var now = DateTime.UtcNow;

                    var hotTokensResult = await hotTokensManager.GetManyAsync();

                    var toUpdate = new List<Token>();
                    var savedTokenAddresses = new List<string>();

                    foreach (var item in hotTokensResult)
                    {
                        var token = item.Token;

                        string name = token.Name;
                        string symbol = token.Symbol;
                        string owner = token.Owner;
                        string pair = token.PairAddress;

                        TokenInfo tokenInfo = token.TokenInfo;
                        List<Holder> holders = token?.Top100Holders;

                        TokenSourceCode sourceCode = token.SourceCode;
                        Pair pairTradingData = token.PairTradingData;

                        DateTime? lastInfoDate = token.LastInfoDate;

                        if (!(token != null && (token.LastInfoDate == null || token.LastInfoDate != null && (now - token.LastInfoDate.Value).TotalSeconds > 12))) continue;

                        if (sourceCode == null || sourceCode.MissingSourceCode() || sourceCode.ABI == Settings.defautltABI)
                        {
                            sourceCode = await TokenService.TryGetSourceCodeAsync(token.Address);
                        }

                        string abi = sourceCode != null && !sourceCode.MissingSourceCode() ? sourceCode.ABI : Settings.defautltABI;
                        var tokenContract = TokenService.TryGetContractWithABI(token.Address, abi);

                        var tasks = new List<Task> {
                            Task.Run(async () => {
                                tokenInfo = await TokenInfoService.TryGetBlockChainData(token.Address);
                            }),
                            Task.Run(async () => {
                                holders = await TokenInfoService.TryGetTopTokenHolders(token.Address, 100);
                            })
                        };

                        if (tokenContract != null)
                        {
                            if (name == null)
                            {
                                tasks.Add(Task.Run(async () =>
                                {
                                    name = await TokenService.TryGetTokenNameAsync(tokenContract);
                                }));
                            }
                            if (symbol == null)
                            {
                                tasks.Add(Task.Run(async () =>
                                {
                                    symbol = await TokenService.TryGetTokenSymbolAsync(tokenContract);
                                }));
                            }

                            tasks.Add(Task.Run(async () =>
                            {
                                owner = await TokenService.TryGetTokenOwnerAsync(tokenContract);
                            }));

                            if (pair == null)
                            {
                                var uniV2FactoryContract = TokenService.TryGetFactoryContract(Settings.uniV2Factory);
                                if (uniV2FactoryContract != null)
                                {
                                    tasks.Add(Task.Run(async () =>
                                    {
                                        pair = await TokenService.TryGetPair(token.Address, Settings.WETH, uniV2FactoryContract);
                                    }));
                                }
                            }
                        }

                        await Task.WhenAll(tasks);

                        if (pair != null)
                        {
                            pairTradingData = await TradingDataService.TryGetTradingDataAsync(pair);
                        }

                        lastInfoDate = now;

                        double? price = null;
                        try
                        {
                            if (pairTradingData != null && double.TryParse(pairTradingData.PriceUsd, out double _price))
                            {
                                price = _price;
                            }
                        }
                        catch { }
                        var tokenPrice = new TokenPrice
                        {
                            Value = price
                        };

                        token.Name = name;
                        token.Symbol = symbol;
                        token.Owner = owner;
                        token.SourceCode = sourceCode;
                        token.PairAddress = pair;
                        token.TokenInfo = tokenInfo;
                        token.Top100Holders = holders;
                        token.PairTradingData = pairTradingData;

                        token.LastInfoDate = lastInfoDate;

                        if (token.Prices.LastOrDefault() == null)
                        {
                            token.Prices = new List<TokenPrice> {
                                    tokenPrice
                                };
                        }
                        else if (token.Prices.LastOrDefault().Value != price)
                        {
                            token.Prices.Add(tokenPrice);
                        }

                        toUpdate.Add(token);

                        if (token.Name != null)
                        {
                            savedTokenAddresses.Add(token.Address);
                        }

                    }

                    var dbTasks = new List<Task>();

                    if (toUpdate.Any())
                    {
                        //dbTasks.AddRange(toUpdate.Select(token => Task.Run(async () =>
                        //{
                        //    await tokensManager.UpdateOneExAsync(token, true);
                        //})));

                        dbTasks.Add(Task.Run(async () =>
                        {
                            await tokensManager.BlukUpdateManyAsync(toUpdate, false);
                        }));
                    }

                    if (savedTokenAddresses.Any())
                    {
                        var addressesArray = savedTokenAddresses.Distinct().ToArray();

                        dbTasks.Add(Task.Run(async () =>
                        {
                            var update = Builders<Txn>.Update.Set(t => t.GotTokenInfo, true);
                            var updateResult = await txnsManager.UpdateManyAsync(t => addressesArray.Contains(t.To) && !t.GotTokenInfo && !t.IsApprove, update);

                            _logger.Write($"updated 'GotTokenInfo' for {updateResult.ModifiedCount} txns");
                        }));
                    }

                    if (dbTasks.Any())
                    {
                        await Task.WhenAll(dbTasks);
                    }


                    await UpdateTelegramHotTokensMessageAsync(hotTokensResult);

                    await CreateTelegramHottestTokenMessageAsync(tokensManager, telegramScoreMessagesManager, hotTokensResult);
                }
                catch (Exception e)
                {
                    _logger.Write($"Error: {e.Message}");
                }

                Thread.Sleep(1000);
            } while (true);

        }

        private static async Task UpdateTelegramHotTokensMessageAsync(HotToken[] hotTokensResult)
        {
            try
            {
                var botClient = new TelegramBotClient("6233741688:AAFsZYsED-vVshmgzfxWErmc81FVsuj-UnQ");
                var chatId = new ChatId(-1001617088627);

                InlineKeyboardMarkup inlineKeyboard = new[]{
                        InlineKeyboardButton.WithUrl(text: "🔥🔥marketviz.app🔥🔥", url: "https://marketviz.app")
                };

                var hotTokens = hotTokensResult.OrderByDescending(_ => _.Score).ToArray();
                string text = "";
                int i = 0;
                foreach (var item in hotTokens)
                {
                    text += $"{++i:D2} - {item.Token.Name} ({item.Token.Symbol}) - score: <b>{item.Score}</b>  \n";
                }

                await botClient.EditMessageTextAsync(chatId, 58, text, ParseMode.Html, replyMarkup: inlineKeyboard);
            }
            catch (Exception e)
            {
                ;
            }
        }

        private static int _hotTokenScoreThreshold = 6;
        //private static int _hotTokenScoreThreshold = 4;
        private static int _hotTokenExpiryTime = 6;
        private static async Task CreateTelegramHottestTokenMessageAsync(
            TokensManager tokensManager, 
            TelegramScoreMessagesManager telegramScoreMessagesManager,
            HotToken[] hotTokensResult)
        {
            try
            {
                var now = DateTime.UtcNow;
                var time = now.AddMinutes(-_hotTokenExpiryTime);

                var botClient = new TelegramBotClient("6233741688:AAFsZYsED-vVshmgzfxWErmc81FVsuj-UnQ");
                //var chatId = new ChatId(-1001701981864); //dev
                var chatId = new ChatId(-1001617088627);

                var fireTokens = hotTokensResult.Where(_ => _.Score >= _hotTokenScoreThreshold).ToList();
                var fireTokenAddresses = fireTokens.Select(_ => _.Token.Address).ToArray();
                var fireTokenMessages = await telegramScoreMessagesManager.GetManyAsync(m => fireTokenAddresses.Contains(m.TokenAddress));

                var messagesToCreate = new List<TelegramScoreMessage>();
                var messagesToUpdate = new List<TelegramScoreMessage>();
                foreach (var item in fireTokens)
                {
                    string text = $"token <b>{item.Token.Name} ({item.Token.Symbol})</b> is on fire 🔥 with a score of <b>{item.Score}</b>" +
                        $" in marketviz.app <i>[{Guid.NewGuid().ToString().Substring(0, 6)}]</i>";

                    InlineKeyboardMarkup inlineKeyboard = new[]{
                            InlineKeyboardButton.WithUrl(text: "marketviz.app", url: "https://marketviz.app"),
                            InlineKeyboardButton.WithUrl(text: "dexscreener", url: $"https://dexscreener.com/ethereum/{item.Token.PairAddress ?? item.Token.Address}")
                    };

                    var existingMessage = fireTokenMessages.FirstOrDefault(m => m.TokenAddress == item.Token.Address);
                    if (existingMessage == null) {
                        try
                        {
                            var message = await botClient.SendTextMessageAsync(chatId, text, ParseMode.Html, disableNotification: false, replyMarkup: inlineKeyboard);

                            messagesToCreate.Add(new TelegramScoreMessage
                            {
                                MessageId = message.MessageId,
                                Date = message.Date,
                                Text = message.Text,
                                Score = item.Score,
                                TokenAddress = item.Token.Address
                            });

                        }
                        catch (Exception e)
                        {
                            ;
                        }
                    }
                    else {
                        try
                        {
                            if (item.Score > existingMessage.Score) {
                                var message = await botClient.EditMessageTextAsync(chatId, existingMessage.MessageId, text, ParseMode.Html, replyMarkup: inlineKeyboard);

                                existingMessage.Date = message.Date;
                                existingMessage.Text = message.Text;
                                existingMessage.Score = item.Score;
                                //existingMessage.UpdatedDate = DateTime.UtcNow;

                                messagesToUpdate.Add(existingMessage);
                            }
                        }
                        catch (Exception e)
                        {
                            ;
                        }
                    }
                }

                if (messagesToCreate.Any()) {
                    await telegramScoreMessagesManager.CreateManyAsync(messagesToCreate.ToArray());
                }

                // TODO: make this faster
                if (messagesToUpdate.Any()) {
                    //await Task.WhenAll(messagesToUpdate.Select(m => Task.Run(async () => {
                    //    await telegramScoreMessagesManager.UpdateOneAsync(m);
                    //})));

                    await telegramScoreMessagesManager.BlukUpdateManyAsync(messagesToUpdate);
                }
            }
            catch (Exception e)
            {
                ;
            }
        }

        private static async void TelegramExpiredScoreMessagesLoop(
            TokensManager tokensManager, 
            TelegramScoreMessagesManager telegramScoreMessagesManager, 
            TelegramOpportunityMessagesManager telegramOpportunityMessagesManager)
        {
            do
            {
                try
                {
                    var botClient = new TelegramBotClient("6233741688:AAFsZYsED-vVshmgzfxWErmc81FVsuj-UnQ");
                    //var chatId = new ChatId(-1001701981864); //dev
                    var chatId = new ChatId(-1001617088627);

                    var now = DateTime.UtcNow;
                    var time = now.AddMinutes(-_hotTokenExpiryTime);

                    var messages = await telegramScoreMessagesManager.GetManyAsync(m => m.UpdatedDate < time);

                    var tokenAddresses = messages.Select(m => m.TokenAddress).ToArray();
                    var scoreResults = await tokensManager.GetTokenScoresAsync(tokenAddresses);

                    var toDeleteIds = new List<string>();
                    foreach (var message in messages)
                    {
                        var scoreResult = scoreResults.FirstOrDefault(_ => _.Token.Address == message.TokenAddress);
                        if (scoreResult == null || scoreResult.Score >= _hotTokenScoreThreshold) continue;

                        try
                        {
                            await botClient.DeleteMessageAsync(chatId, message.MessageId);
                        }
                        catch (Exception e)
                        {
                            ;
                        }

                        var token = scoreResult.Token;

                        var pricesWindow = token.Prices
                            .Where(tp => tp.Value != null && tp.Value != 0)
                            .Where(tp => tp.Date >= message.CreatedDate.AddMinutes(-12))
                            .ToArray();

                        var opportunity = pricesWindow.ToMaxOpportunity();

                        if (opportunity != null)
                        {
                            var minPrice = opportunity.Min;
                            var maxPrice = opportunity.Max;

                            double ratio = (double)maxPrice.Value / (double)minPrice.Value;

                            if (ratio > 1)
                            {
                                InlineKeyboardMarkup inlineKeyboard = new[]{
                                            InlineKeyboardButton.WithUrl(text: "marketviz.app", url: "https://marketviz.app"),
                                            InlineKeyboardButton.WithUrl(text: "dexscreener", url: $"https://dexscreener.com/ethereum/{token.PairAddress ?? token.Address}"),
                                    };

                                bool opportunityMessageExists = await telegramOpportunityMessagesManager.AnyAsync(tm => tm.OriginMessageId == message.Id);
                                if (!opportunityMessageExists)
                                {
                                    string text = $"there was an opportunity of making <b>{Math.Round(ratio * 100),2}%</b> by buying <b>{token.Name} ({token.Symbol})</b>" +
                                        $" at <b>{minPrice.Date:R}</b> and selling it at <b>{maxPrice.Date:R}</b> using marketviz.app";

                                    Message opportunityMessage = null;
                                    if (ratio >= 1.2)
                                    {
                                        try
                                        {
                                            opportunityMessage = await botClient.SendTextMessageAsync(chatId, text, ParseMode.Html, disableNotification: true, replyMarkup: inlineKeyboard);
                                        }
                                        catch (Exception e)
                                        {
                                            ;
                                        }
                                    }

                                    await telegramOpportunityMessagesManager.CreateOneAsync(new TelegramOpportunityMessage
                                    {
                                        Date = opportunityMessage?.Date,
                                        MessageId = opportunityMessage?.MessageId,
                                        Text = text,
                                        TokenAddress = token.Address,
                                        MinPrice = minPrice,
                                        MaxPrice = maxPrice,
                                        OriginMessageId = message.Id
                                    });
                                }
                            }
                        }

                        toDeleteIds.Add(message.Id);
                    }

                    await telegramScoreMessagesManager.DeleteManyAsync(m => toDeleteIds.Contains(m.Id));
                }
                catch (Exception e)
                {
                    _logger.Write($"Error: {e.Message}");
                }

                //Thread.Sleep(360000);
                Thread.Sleep(60000);
            } while (true);

        }

    }

    public class HottestToken
    {
        public Token Token { get; set; }
        public double Score { get; set; }
        public DateTime SentMessageDate { get; set; }
    }
}
