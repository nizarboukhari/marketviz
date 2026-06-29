using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Osmos.Core.ApiClient
{
    public class ApiRequestOptions
    {
        public string Uri { get; set; }
        public HttpMethod Method { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public class ApiRequest
    {
        public ApiRequest()
        {
        }

        public ApiRequest(ApiRequestOptions options)
        {
            if (options == null || options.Uri == null || options.Method == null) throw new ArgumentNullException();

            Uri = options.Uri;
            Method = options.Method;
            Headers = options.Headers;
            Content = options.Content;
        }

        public string Content { get; set; }
        public Dictionary<string, string> Headers { get; }
        public HttpMethod Method { get; set; }
        public string Uri { get; set; }

        public DateTimeOffset RequestDate { get; set; }
        public DateTimeOffset ResponseDate { get; set; }

        public double Duration => (ResponseDate - RequestDate).TotalSeconds;

        public HttpRequestMessage ToHttpRequestMessage()
        {
            var requestMessage = new HttpRequestMessage(Method, Uri);

            if (Headers != null)
            {
                foreach (var header in Headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            if (Content != null)
            {
                requestMessage.Content = new StringContent(Content, Encoding.Default, "application/json");
            }

            return requestMessage;
        }
    }
}
