using System.Threading.Tasks;

namespace Osmos.Core.Helpers.Services
{
    public interface ISmsSender
    {
        Task SendAsync(string to, string message);

        Task SendConfirmPhoneNumberCodeAsync(string to);

        Task<bool> ConfirmPhoneNumberCodeAsync(string to, string code);
    }
}
