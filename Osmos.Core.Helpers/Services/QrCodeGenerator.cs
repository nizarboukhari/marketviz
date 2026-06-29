using QRCoder;
using System.Drawing;

namespace Osmos.Core.Helpers.Services
{
    public class QrCodeGenerator
    {
        public static string GenerateUserQrCode(string userId)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(userId, QRCodeGenerator.ECCLevel.Q);
            Base64QRCode qrCode = new Base64QRCode(qrCodeData);

            //var image = (Bitmap)Bitmap.FromFile(@"App_Data/Images/pattern.png");

            string qrCodeImageAsBase64 = qrCode.GetGraphic(20, Color.Black, Color.White);

            return "data:image/png;base64," + qrCodeImageAsBase64;
        }
    }
}
