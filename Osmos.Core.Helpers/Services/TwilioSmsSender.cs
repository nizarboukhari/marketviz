using Microsoft.Extensions.Options;
using Osmos.Core.Helpers.Models;
using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Verify.V2.Service;
using Twilio.Types;

namespace Osmos.Core.Helpers.Services
{
    public class TwilioSmsSender : ISmsSender
    {
        public async Task SendAsync(string to, string message)
        {
            TwilioClient.Init(_twilioOptions.AccountSID, _twilioOptions.AuthToken);

            var messageResource = await MessageResource.CreateAsync(
                new PhoneNumber(to),
                from: new PhoneNumber(_twilioOptions.Number),
                body: message
            );
        }

        public async Task SendConfirmPhoneNumberCodeAsync(string to)
        {
            TwilioClient.Init(_twilioOptions.AccountSID, _twilioOptions.AuthToken);

            var verification = await VerificationResource.CreateAsync(
                to: to,
                channel: "sms",
                pathServiceSid: _twilioOptions.ServiceID
            );
        }

        public async Task<bool> ConfirmPhoneNumberCodeAsync(string to, string code){

            TwilioClient.Init(_twilioOptions.AccountSID, _twilioOptions.AuthToken);

            VerificationCheckResource verificationCheck;
            try
            {
                verificationCheck = await VerificationCheckResource.CreateAsync(
                    to: to,
                    code: code,
                    pathServiceSid: _twilioOptions.ServiceID
                );   
            }
            catch
            {
                return false;
            }


            return verificationCheck.Status == "approved";
        }

        #region internals

        private TwilioOptions _twilioOptions = null;

        public TwilioSmsSender(IOptions<TwilioOptions> twilioOptionsAccessor)
        {
            _twilioOptions = twilioOptionsAccessor.Value;
        }

        #endregion
    }
}
