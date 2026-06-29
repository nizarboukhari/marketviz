using System;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class TelegramHotMessage : TelegramMessage
    {
        public string Text { get; set; }
        public bool Pinned { get; set; }
        public DateTime UpdatedDate { get; set; }

        public TelegramHotMessage()
        {
            UpdatedDate = DateTime.UtcNow;
        }
    }
}
