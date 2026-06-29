using Osmos.Core.Data;

namespace Osmos.Business.Data.NoSql.Entities
{
    public abstract class TelegramMessage : Entity
    {
        public TelegramMessageInfo[] MessagesInfos { get; set; } = new TelegramMessageInfo[] { };
    }

    public class TelegramMessageInfo
    {
        public int MessageId { get; set; }
        public string ChatName { get; set; }
        public string ChatId { get; set; }
    }
}
