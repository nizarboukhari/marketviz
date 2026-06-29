using System;

namespace Osmos.Core.ApiClient
{
    public class ApiCall
    {
        public ApiCall(ApiRequest apiRequestMessage, object extra = null)
        {
            ApiRequest = apiRequestMessage;
            Extra = extra;
        }
        public ApiRequest ApiRequest { get; set; }
        public ApiResponse ApiResponse { get; set; }

        public int Iterations { get; set; }

        public bool Succeeded { get; set; }
        public Exception Exception { get; set; }

        public DateTimeOffset RequestDate { get; set; }
        public DateTimeOffset ResponseDate { get; set; }

        public double Duration => (ResponseDate - RequestDate).TotalSeconds;

        public object Extra { get; set; }
    }
}
