using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Osmos.Core.ApiClient
{
    public class ApiClient
    {
        public int MaxIterations { get; }
        public int Iterations { get; private set; }

        public Dictionary<string, ApiCall> ApiCalls { get; }

        public int Timeout { get; }
        public DateTimeOffset StartTime { get; private set; }
        public DateTimeOffset FinishTime { get; private set; }
        public double Duration => (FinishTime - StartTime).TotalSeconds;

        public bool AllSucceeded => ApiCalls.Keys.All(key => ApiCalls[key].Succeeded);

        public ApiClient(ApiClientOptions options)
        {

            Iterations = 0;
            MaxIterations = 3;
            Timeout = 10;

            ApiCalls = new Dictionary<string, ApiCall>();

            if (options.MaxIterations != null) MaxIterations = (int)options.MaxIterations;
            if (options.Timeout != null) Timeout = (int)options.Timeout;

        }

        public void Add(string key, ApiRequest apiRequest, object extra = null)
        {
            ApiCalls.Add(key, new ApiCall(apiRequest, extra));
        }

        public async Task ExecuteAsync()
        {
            var httpRequestMessages = ApiCalls.Keys.Select(key => ApiCalls[key]);
            StartTime = DateTimeOffset.UtcNow;
            await _ExecuteRequestsAsync(httpRequestMessages);
            FinishTime = DateTimeOffset.UtcNow;
        }

        public static async Task<ApiCall> ExecuteSingleAsync(ApiRequest apiRequest)
        {
            var apiClient = new ApiClient(new ApiClientOptions
            {
                MaxIterations = 0
            });

            apiClient.Add("0", apiRequest);

            await apiClient.ExecuteAsync();

            return apiClient.ApiCalls["0"];
        }

        #region internals

        private async Task _ExecuteRequestsAsync(IEnumerable<ApiCall> apiCalls)
        {
            if (apiCalls == null || !apiCalls.Any()) throw new ArgumentNullException();

            var tasks = apiCalls.Select(apiCall => Task.Run(async () => await _ExcecuteRequestAsync(apiCall)));
            await Task.WhenAll(tasks);

            Iterations++;

            var failedApiCalls = new List<ApiCall>();
            foreach (var key in ApiCalls.Keys)
            {
                if (!ApiCalls[key].Succeeded)
                {
                    ApiCalls[key].Iterations = Iterations;
                    failedApiCalls.Add(ApiCalls[key]);
                }
            }

            if (Iterations >= MaxIterations)
            {
                return;
            }

            if (failedApiCalls.Any())
            {
                Console.WriteLine($"{failedApiCalls.Count}");
                await _ExecuteRequestsAsync(failedApiCalls);
            }
        }

        private async Task _ExcecuteRequestAsync(ApiCall apiCall)
        {
            if (apiCall == null || apiCall.ApiRequest == null) throw new ArgumentNullException();

            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(Timeout)
            };

            var httpRequestMessage = apiCall.ApiRequest.ToHttpRequestMessage();
            var apiResponse = new ApiResponse();

            try
            {
                apiCall.RequestDate = DateTime.UtcNow;
                var response = await httpClient.SendAsync(httpRequestMessage);
                apiCall.ResponseDate = DateTime.UtcNow;
                apiCall.Succeeded = response.IsSuccessStatusCode;

                var streamContent = await response.Content.ReadAsStreamAsync();
                string contentAsString = await response.Content.ReadAsStringAsync();

                apiResponse.StreamContent = streamContent;
                apiResponse.Headers = response.Headers;
                apiResponse.StringContent = contentAsString;
                apiResponse.StatusCode = response.StatusCode;

                httpClient.Dispose();
            }
            catch (Exception e)
            {
                apiCall.Succeeded = false;
                apiResponse.Exception = e;

                httpClient.Dispose();
            }

            apiCall.ApiResponse = apiResponse;
        }

        #endregion
    }
}
