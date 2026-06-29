using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data;
using Osmos.Workers.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Osmos.Workers.Messenger
{
    public class TelegramManager
    {
        public async Task<ChatMessage[]> SentMessageAsync(string text, bool? disableNotification = null, InlineKeyboardMarkup replyMarkup = null)
        {
            var result = _chats
                .Select(_ => new ChatMessage
                {
                    ChatId = _.ChatId,
                    ChatName = _.Name
                })
                .ToArray();

            if (string.IsNullOrEmpty(text)) return result;

            string _text = text + $"  \n<i>[{UtcNowTimestamp()}]</i>";

            var tasks = result.Select(_ => Task.Run(async () =>
            {
                try
                {
                    _.Message = await _botClient.SendTextMessageAsync(_.ChatId, _text, ParseMode.Html, disableNotification: disableNotification, replyMarkup: replyMarkup);
                    _.MessageId = _.Message.MessageId;
                }
                catch (Exception e)
                {
                    _.Exception = e;
                }
            }));

            await Task.WhenAll(tasks);

            return result;
        }

        public async Task<ChatMessage[]> EditMessageAsync(ChatMessage[] messages, string text, InlineKeyboardMarkup replyMarkup = null)
        {
            var result = messages
                .Select(_ => new ChatMessage
                {
                    ChatId = _.ChatId,
                    ChatName = _.ChatName,
                    MessageId = _.MessageId
                })
                .ToArray();

            if (string.IsNullOrEmpty(text)) return result;

            string _text = text + $"  \n<i>[{UtcNowTimestamp()}]</i>";

            var tasks = result.Select(_ => Task.Run(async () =>
            {
                try
                {
                    _.Message = await _botClient.EditMessageTextAsync(_.ChatId, _.MessageId, _text, ParseMode.Html, replyMarkup: replyMarkup);
                }
                catch (Exception e)
                {
                    _.Exception = e;
                }
            }));

            await Task.WhenAll(tasks);

            return result;
        }

        public async Task UpdateTelegramHotTokensMessageAsync(HotToken[] hotTokensResult, int hotTokenScoreMaxThreshold)
        {
            InlineKeyboardMarkup inlineKeyboard = new[]{
                        InlineKeyboardButton.WithUrl(text: "🔥🔥marketviz.app🔥🔥", url: "https://marketviz.app")
                };

            var hotTokens = hotTokensResult.OrderByDescending(_ => _.Score).ToArray();

            string text = "Check live transactions and more stats on <a href=\"https://www.marketviz.app\">marketviz.app</a> \n \n";
            int i = 1;
            foreach (var item in hotTokens)
            {
                string rank = $"{i}";
                switch (i)
                {
                    case 1:
                        rank = "🥇";
                        break;
                    case 2:
                        rank = "🥈";
                        break;
                    case 3:
                        rank = "🥉";
                        break;
                    default:
                        break;
                }
                if (3 < i && i < 10) rank = $"  {rank}";
                text += $"{rank} - <b>{item.Token.Name} ({item.Token.Symbol})</b> <a href=\"https://dexscreener.com/ethereum/{item.Token.Address}\">📊</a> - score: <b>{item.Score}</b> \n" 
                    + $"<code>{item.Token.Address}</code>";
                if (i < hotTokensResult.Length) text += "  \n";
                i++;
            }
            text += " \n \n https://t.me/marketvizcalls to stay notified on tg";

            var options = new QueryOptions<TelegramHotMessage>
            {
                Pagination = new QueryPaginationOptions
                {
                    Size = 1,
                    Page = 1
                },
                SortOptions = new QuerySortOptions<TelegramHotMessage>
                {
                    Descending = true,
                    Field = m => m.UpdatedDate
                }
            };
            var result = await _telegramHotMessagesManager.GetManyAsync(options, t => t.Pinned);

            var entityMessage = result.Entities.FirstOrDefault();

            if (entityMessage != null && entityMessage.MessagesInfos.Any() && (DateTime.UtcNow - entityMessage.CreatedDate).TotalMinutes < 6)
            {
                var messages = entityMessage.MessagesInfos
                    .Select(_ => new ChatMessage
                    {
                        ChatId = _.ChatId,
                        ChatName = _.ChatName,
                        MessageId = _.MessageId
                    })
                    .ToArray();


                var editMessageResult = await EditMessageAsync(messages, text, replyMarkup: inlineKeyboard);

                entityMessage.MessagesInfos = editMessageResult
                    .Select(_ => new TelegramMessageInfo
                    {
                        ChatId = _.ChatId,
                        ChatName = _.ChatName,
                        MessageId = _.MessageId
                    })
                    .ToArray();
                entityMessage.Text = text;
                entityMessage.UpdatedDate = DateTime.UtcNow;

                await _telegramHotMessagesManager.UpdateOneAsync(entityMessage);

                Console.WriteLine("updated hot tokens message with new values");
            }
            else if (hotTokens.First().Score >= hotTokenScoreMaxThreshold)
            {
                if (entityMessage != null && entityMessage.MessagesInfos.Any())
                {
                    foreach (var item in entityMessage.MessagesInfos)
                    {
                        await ActionsHelper.SmoothRunAsync(async () => {
                            await _botClient.UnpinChatMessageAsync(item.ChatId, item.MessageId);
                        });
                    }
                }

                var sendMessagesResult = await SentMessageAsync(text, disableNotification: true, replyMarkup: inlineKeyboard);

                entityMessage = new TelegramHotMessage
                {
                    MessagesInfos = sendMessagesResult
                                        .Select(_ => new TelegramMessageInfo
                                        {
                                            ChatId = _.ChatId,
                                            ChatName = _.ChatName,
                                            MessageId = _.MessageId
                                        })
                                        .ToArray(),
                    Text = text,
                    Pinned = true
                };
                await _telegramHotMessagesManager.CreateOneAsync(entityMessage);

                foreach (var item in entityMessage.MessagesInfos)
                {
                    await ActionsHelper.SmoothRunAsync(async () => {
                        await _botClient.PinChatMessageAsync(item.ChatId, item.MessageId, true);
                    });
                }

                Console.WriteLine("created hot tokens message with new values");
            }
        }

        public async Task CreateTelegramHottestTokenMessageAsync(HotToken[] hotTokensResult, int hotTokenScoreMaxThreshold, int hotTokenExpiryTime)
        {
            var now = DateTime.UtcNow;
            var time = now.AddMinutes(-hotTokenExpiryTime);

            var fireTokens = hotTokensResult.Where(_ => _.Score >= hotTokenScoreMaxThreshold).ToList();
            var fireTokenAddresses = fireTokens.Select(_ => _.Token.Address).ToArray();
            var fireTokenMessages = await _telegramScoreMessagesManager.GetManyAsync(m => fireTokenAddresses.Contains(m.TokenAddress));

            var messagesToCreate = new List<TelegramScoreMessage>();
            var messagesToUpdate = new List<TelegramScoreMessage>();
            foreach (var item in fireTokens)
            {
                bool renouncedOwnership = item.Token.Owner == "0x0000000000000000000000000000000000000000";
                bool sourceCodeExists = item.Token.SourceCode != null && !item.Token.SourceCode.Missing;

                string text = $"token <b>{item.Token.Name} ({item.Token.Symbol})</b> is on fire 🔥 with a score of <b>{item.Score}</b>  \n\n";
                if (item.Token.PairTradingData != null) {
                    double.TryParse(item.Token.PairTradingData.PriceUsd, out double priceUsd);
                    text += $"💲 price: <b>${priceUsd.Nice(6)}</b>  \n";
                    text += $"📈 market cap: <b>${item.Token.PairTradingData.Fdv.Nice(6)}</b>  \n";
                    if (item.Token.PairTradingData.Liquidity != null) {
                        text += $"💰 liquidity: <b>${item.Token.PairTradingData.Liquidity.Usd.Nice(6)}</b>  \n";
                    }
                    text += "\n";
                }
                text += renouncedOwnership ? "✅ <b>renounced ownership</b>  \n" : "❌ <b>still has an owner</b>  \n";
                text += sourceCodeExists ? "✅ <b>source code exists</b>  \n" : "❌ <b>source code not available</b>  \n";
                text += "\n";

                text += "marketviz.app";

                InlineKeyboardMarkup inlineKeyboard = new[]{
                            InlineKeyboardButton.WithUrl(text: "marketviz.app", url: "https://marketviz.app"),
                            InlineKeyboardButton.WithUrl(text: "dexscreener", url: $"https://dexscreener.com/ethereum/{item.Token.PairAddress ?? item.Token.Address}"),
                            InlineKeyboardButton.WithUrl(text: "tokensniffer", url: $"https://tokensniffer.com/token/eth/{item.Token.Address}")
                    };

                var existingMessage = fireTokenMessages.FirstOrDefault(m => m.TokenAddress == item.Token.Address);
                if (existingMessage == null || !existingMessage.MessagesInfos.Any())
                {
                    var sendMessagesResult = await SentMessageAsync(text, disableNotification: false, replyMarkup: inlineKeyboard);

                    messagesToCreate.Add(new TelegramScoreMessage
                    {
                        MessagesInfos = sendMessagesResult
                                    .Select(_ => new TelegramMessageInfo
                                    {
                                        ChatId = _.ChatId,
                                        ChatName = _.ChatName,
                                        MessageId = _.MessageId
                                    })
                                    .ToArray(),
                        Text = text,
                        Score = item.Score,
                        TokenAddress = item.Token.Address
                    });

                    Console.WriteLine($"sent message {text}");
                }
                else
                {
                    if (item.Score > existingMessage.Score)
                    {
                        var messages = existingMessage.MessagesInfos
                            .Select(_ => new ChatMessage
                            {
                                ChatId = _.ChatId,
                                ChatName = _.ChatName,
                                MessageId = _.MessageId
                            })
                            .ToArray();


                        var editMessageResult = await EditMessageAsync(messages, text, replyMarkup: inlineKeyboard);

                        existingMessage.MessagesInfos = editMessageResult
                            .Select(_ => new TelegramMessageInfo
                            {
                                ChatId = _.ChatId,
                                ChatName = _.ChatName,
                                MessageId = _.MessageId
                            })
                            .ToArray();
                        existingMessage.Text = text;
                        existingMessage.UpdatedDate = DateTime.UtcNow;
                        existingMessage.Score = item.Score;

                        messagesToUpdate.Add(existingMessage);

                        Console.WriteLine($"updated message {text}");
                    }
                }
            }

            if (messagesToCreate.Any())
            {
                await _telegramScoreMessagesManager.CreateManyAsync(messagesToCreate.ToArray());
            }

            if (messagesToUpdate.Any())
            {
                await _telegramScoreMessagesManager.BlukUpdateManyAsync(messagesToUpdate);
            }
        }

        public async Task TelegramExpiredScoreMessagesLoop(int hotTokenScoreMinThreshold, int hotTokenExpiryTime)
        {
            var now = DateTime.UtcNow;
            var time = now.AddMinutes(-hotTokenExpiryTime);

            var messages = await _telegramScoreMessagesManager.GetManyAsync(m => m.UpdatedDate < time && !m.Deleted);

            var tokenAddresses = messages.Select(m => m.TokenAddress).Distinct().ToArray();

            var scoreResults = await _tokensManager.GetTokenScoresAsync(tokenAddresses);

            var toDelete = new List<TelegramScoreMessage>();
            foreach (var message in messages)
            {
                var scoreResult = scoreResults.FirstOrDefault(_ => _.Token.Address == message.TokenAddress);
                if (scoreResult == null || scoreResult.Score >= hotTokenScoreMinThreshold) continue;

                //await ActionsHelper.SmoothRunAsync(async () =>
                //{
                //    await botClient.DeleteMessageAsync(chatId, message.MessageId);
                //});

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
                                            InlineKeyboardButton.WithUrl(text: "tokensniffer", url: $"https://tokensniffer.com/token/eth/{token.Address}")
                                    };

                        bool opportunityMessageExists = await _telegramOpportunityMessagesManager.AnyAsync(tm => tm.OriginMessageId == message.Id);
                        if (!opportunityMessageExists)
                        {
                            string text = $"there was an opportunity of making <b>{Math.Round(ratio * 100),2}%</b> by buying <b>{token.Name} ({token.Symbol})</b>" +
                                $"  \nat <b>{minPrice.Date:R}</b>  \nand selling it  \nat <b>{maxPrice.Date:R}</b>\n" +
                                $"marketviz.app";

                            ChatMessage[] sendMessagesResult = null;
                            if (ratio >= 1.2)
                            {
                                await ActionsHelper.SmoothRunAsync(async () => {
                                    sendMessagesResult = await SentMessageAsync(text, disableNotification: true, replyMarkup: inlineKeyboard);
                                });
                            }

                            await _telegramOpportunityMessagesManager.CreateOneAsync(new TelegramOpportunityMessage
                            {
                                MessagesInfos = sendMessagesResult == null ? new TelegramMessageInfo[] { } :
                                                sendMessagesResult
                                                    .Select(_ => new TelegramMessageInfo
                                                    {
                                                        ChatId = _.ChatId,
                                                        ChatName = _.ChatName,
                                                        MessageId = _.MessageId
                                                    })
                                                    .ToArray(),
                                Text = text,
                                TokenAddress = token.Address,
                                MinPrice = minPrice,
                                MaxPrice = maxPrice,
                                OriginMessageId = message.Id
                            });
                        }
                    }
                }

                toDelete.Add(message);
            }

            await _telegramScoreMessagesManager.UpdateManyAsync(toDelete.ToArray(), Builders<TelegramScoreMessage>.Update.Set(m => m.Deleted, true));

            Console.WriteLine($"deleted {toDelete.Count} expired opportunity messages");
        }

        #region internals

        private readonly TelegramSettings _telegramSettings = null;
        private readonly TelegramBotClient _botClient = null;
        private readonly Chat[] _chats = null;

        private readonly TokensManager _tokensManager = null;
        private readonly TelegramScoreMessagesManager _telegramScoreMessagesManager = null;
        private readonly TelegramOpportunityMessagesManager _telegramOpportunityMessagesManager = null;
        private readonly TelegramHotMessagesManager _telegramHotMessagesManager = null;

        public TelegramManager(
            TokensManager tokensManager,
            IOptions<TelegramSettings> telegramSettings,
            TelegramScoreMessagesManager telegramScoreMessagesManager,
            TelegramOpportunityMessagesManager telegramOpportunityMessagesManager,
            TelegramHotMessagesManager telegramHotMessagesManager)
        {
            _tokensManager = tokensManager;
            _telegramSettings = telegramSettings.Value;
            _botClient = new TelegramBotClient(_telegramSettings.BotToken);
            _chats = _telegramSettings.Chats.Where(c => c.Active).ToArray();

            _telegramScoreMessagesManager = telegramScoreMessagesManager;
            _telegramOpportunityMessagesManager = telegramOpportunityMessagesManager;
            _telegramHotMessagesManager = telegramHotMessagesManager;
        }

        private long UtcNowTimestamp()
        {
            return ConvertToTimestamp(DateTime.UtcNow);
        }

        private long ConvertToTimestamp(DateTime value)
        {
            long epoch = (value.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        #endregion
    }

    public class ChatMessage : TelegramMessageInfo
    {
        public Message Message { get; set; }
        public Exception Exception { get; set; }
    }

    public static class NumberExtension {

        static readonly string[] prefixes = { "f", "a", "p", "n", "μ", "m", string.Empty, "k", "M", "G", "T", "P", "E" };

        public static string Nice(this double x, int significant_digits)
        {
            //Check for special numbers and non-numbers
            if (double.IsInfinity(x) || double.IsNaN(x) || x == 0 || significant_digits <= 0)
            {
                return x.ToString();
            }
            // extract sign so we deal with positive numbers only
            int sign = Math.Sign(x);
            x = Math.Abs(x);
            // get scientific exponent, 10^3, 10^6, ...
            int sci = x == 0 ? 0 : (int)Math.Floor(Math.Log(x, 10) / 3) * 3;
            // scale number to exponent found
            x = x * Math.Pow(10, -sci);
            // find number of digits to the left of the decimal
            int dg = x == 0 ? 0 : (int)Math.Floor(Math.Log(x, 10)) + 1;
            // adjust decimals to display
            int decimals = Math.Min(significant_digits - dg, 15);
            // format for the decimals
            string fmt = new string('0', decimals);
            if (sci == 0)
            {
                //no exponent
                return string.Format("{0}{1:0." + fmt + "}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals));
            }
            // find index for prefix. every 3 of sci is a new index
            int index = sci / 3 + 6;
            if (index >= 0 && index < prefixes.Length)
            {
                // with prefix
                return string.Format("{0}{1:0." + fmt + "}{2}",
                    sign < 0 ? "-" : string.Empty,
                    Math.Round(x, decimals),
                    prefixes[index]);
            }
            // with 10^exp format
            return string.Format("{0}{1:0." + fmt + "}·10^{2}",
                sign < 0 ? "-" : string.Empty,
                Math.Round(x, decimals),
                sci);
        }
    }
}
