using System;

namespace Osmos.Business.Worker.Services
{
    public class Logger
    {
        public void Write(string message, string methodName = null) {

            if (!Active) return;

            string header = $"{CallerName}";
            
            if (methodName != null) header += $"|{methodName}";

            Console.WriteLine($"{header} ==> {message}");
        }

        public string CallerName { get; }
        public bool Active { get; set; }

        public Logger(string callerName, bool active = true)
        {
            CallerName = callerName;
            Active = active;
        }
    }
}
