using Osmos.Core.Data;
using System;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class TelegramScoreMessage: TelegramMessage
    {
        public string Text { get; set; }
        public string TokenAddress { get; set; }
        public double Score { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool Deleted { get; set; }

        public TelegramScoreMessage()
        {
            UpdatedDate = DateTime.UtcNow;
        }
    }
}
