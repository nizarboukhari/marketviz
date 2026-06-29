using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Osmos.Core.Helpers.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Core.Helpers
{
    public static class Utils
    {
        public static string GetConfirmEmailUrl(AppOptions appOptions, string userId, string code)
        {
            string result = $"{appOptions.AppUrl}{appOptions.ConfirmEmailRoute}?userId={userId}&code={WebUtility.UrlEncode(code)}";
            return result;
        }

        public static string GetResetPasswordUrl(AppOptions appOptions, string email, string code)
        {
            string result = $"{appOptions.AppUrl}{appOptions.ResetPasswordRoute}?email={email}&code={WebUtility.UrlEncode(code)}";
            return result;
        }

        public static string GetRegisterUrl(AppOptions appOptions, string userId, string code)
        {
            string result = $"{appOptions.AppUrl}{appOptions.RegisterRoute}?userId={userId}&code={WebUtility.UrlEncode(code)}";
            return result;
        }

        public static long Timestamp()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan elapsedTime = DateTime.UtcNow - epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static string ByteToHumanReadable(this long len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return len.ToString() + " " + sizes[order];
        }

        public static byte[] ToBytes<TObject>(this TObject obj)
            where TObject : class
        {
            string json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            byte[] bytes = Encoding.ASCII.GetBytes(json);

            return bytes;
        }

        public static TObject ToObject<TObject>(this Stream stream)
            where TObject : class
        {
            string json = null;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                json = Encoding.ASCII.GetString(bytes);
            }

            var result = JsonConvert.DeserializeObject<TObject>(json);

            return result;
        }

        public static bool IsFileImage(string fileName)
        {
            List<string> imageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG", ".SVG" };

            return imageExtensions.Contains(Path.GetExtension(fileName).ToUpperInvariant());
        }
    }
}
