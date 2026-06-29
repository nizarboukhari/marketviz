using Osmos.Core.Data;
using System;

namespace Osmos.Business.Data.NoSql.Entities
{
    public class TelegramOpportunityMessage : TelegramMessage
    {
        public string Text { get; set; }
        public string TokenAddress { get; set; }
        public TokenPrice MinPrice { get; set; }
        public TokenPrice MaxPrice { get; set; }
        public string OriginMessageId { get; set; }
    }
}
