using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Workers.Common
{
    public static class RealTimeExtension
    {
        public static async Task<bool> SmoothInvokeAsync(this HubConnection connection, string methodName, object obj = null)
        {
            if (connection.State == HubConnectionState.Connected)
            {
                try
                {
                    if (obj == null) await connection.InvokeAsync(methodName);
                    else await connection.InvokeAsync(methodName, obj);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"can not invoke method \"{methodName}\" because {e.Message}");
                    return false;
                }
                return true;
            }

            Console.WriteLine($"rt is down, reconnecting..");
            try
            {
                await connection.StartAsync();

                if (obj == null) await connection.InvokeAsync(methodName);
                else await connection.InvokeAsync(methodName, obj);
            }
            catch (Exception e)
            {
                Console.WriteLine($"can not invoke method \"{methodName}\" because {e.Message}");
                return false;
            }
            return true;
        }
    }
}
