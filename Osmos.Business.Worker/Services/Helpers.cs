using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    public static class Helpers
    {
        public static async Task<string> RequestData(string uri)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(uri);
        }

        public static string ToFunctionName(this string input) {
            string name = input.Replace("0x", "");
            name = name.Substring(0, Math.Min(8, name.Length));
            return name;
        }
    }
}
