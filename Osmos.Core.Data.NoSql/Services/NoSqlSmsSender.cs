using Osmos.Core.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Managers;
using Osmos.Core.Helpers.Services;
using System.Threading.Tasks;

namespace Osmos.Core.Data.NoSql.Services
{
    public class NoSqlSmsSender : ISmsSender
    {
        public async Task SendAsync(string to, string message)
        {
            var smsMessage = new SmsMessage
            {
                To = to,
                Message = message
            };

            smsMessage = await _smsMessagesManager.CreateOneAsync(smsMessage);
        }

        Task ISmsSender.SendConfirmPhoneNumberCodeAsync(string to)
        {
            throw new System.NotImplementedException();
        }

        Task<bool> ISmsSender.ConfirmPhoneNumberCodeAsync(string to, string code)
        {
            throw new System.NotImplementedException();
        }

        private readonly SmsMessagesManager _smsMessagesManager = null;

        public NoSqlSmsSender(SmsMessagesManager smsMessagesManager)
        {
            _smsMessagesManager = smsMessagesManager;
        }
    }
}
