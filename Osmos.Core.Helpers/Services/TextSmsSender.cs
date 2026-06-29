using System.IO;
using System.Threading.Tasks;

namespace Osmos.Core.Helpers.Services
{
    public class TextSmsSender : ISmsSender
    {
        private string _folderPath = @"App_Data/Smss";

        public async Task SendAsync(string to, string message)
        {
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            string fileName = $"{to}-{Utils.Timestamp()}.html";
            string filePath = Path.Combine(_folderPath, fileName);

            await File.WriteAllTextAsync(filePath, message);
        }

        Task<bool> ISmsSender.ConfirmPhoneNumberCodeAsync(string to, string code)
        {
            throw new System.NotImplementedException();
        }

        Task ISmsSender.SendConfirmPhoneNumberCodeAsync(string to)
        {
            throw new System.NotImplementedException();
        }
    }
}
