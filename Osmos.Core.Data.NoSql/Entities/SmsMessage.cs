namespace Osmos.Core.Data.NoSql.Entities
{
    public class SmsMessage : Entity
    {
        public string To { get; set; }
        public string Message { get; set; }
    }
}
