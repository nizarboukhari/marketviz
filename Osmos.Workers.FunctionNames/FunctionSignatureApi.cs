using Newtonsoft.Json;
using Osmos.Core.ApiClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Osmos.Workers.FunctionNames
{
    public static class FunctionSignatureApi
    {
        public static async Task<string> GetAsync(string signature)
        {
            var options = new ApiRequestOptions
            {
                Uri = $"https://www.4byte.directory/api/v1/signatures/?hex_signature=0x{signature}",
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);

            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return null;

            FunctionSignatureApiRootContent content;
            try
            {
                content = JsonConvert.DeserializeObject<FunctionSignatureApiRootContent>(response.ApiResponse.StringContent);
            }
            catch 
            {
                return null;
            }

            if (content == null || content.Results == null || !content.Results.Any()) return null;

            string name = content.Results.Last()?.Name;
            if (name == null) return null;

            int parIndex = name.IndexOf("(");
            if (parIndex > 0) {
                name = name.Substring(0, parIndex);
            }

            return name;
        }
    }

    public class FunctionSignatureApiRootContent {
        public FunctionSignatureApiItem[] Results { get; set; }
    }

    public class FunctionSignatureApiItem
    {
        [JsonProperty("text_signature")]
        public string Name { get; set; }
        [JsonProperty("hex_signature")]
        public string Signature { get; set; }
    }
}
