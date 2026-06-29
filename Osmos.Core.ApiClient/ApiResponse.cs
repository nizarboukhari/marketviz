using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Osmos.Core.ApiClient
{
    public class ApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StringContent { get; set; }
        public HttpResponseHeaders Headers { get; set; }
        public Exception Exception { get; set; }

        public Stream StreamContent { get; set; }

        public HttpResponseMessage ToHttpResponseMessage()
        {
            var httpResponseMessage = new HttpResponseMessage(StatusCode);

            if (Headers != null)
            {
                foreach (var header in Headers.ToList())
                {
                    httpResponseMessage.Headers.Add(header.Key, header.Value);
                }
            }

            httpResponseMessage.Content = new StringContent(StringContent);

            return httpResponseMessage;
        }
    }
}
