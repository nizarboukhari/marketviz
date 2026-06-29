using Telegram.Bot.Types;

namespace Osmos.Workers.Messenger
{
    public class TelegramSettings
    {
        public string BotToken { get; set; }
        public Chat[] Chats { get; set; }
    }

    public class Chat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public ChatId ChatId
        {
            get
            {
                return new ChatId(Id);
            }
        }
    }
}
