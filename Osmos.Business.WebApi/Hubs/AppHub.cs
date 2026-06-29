using Microsoft.AspNetCore.SignalR;
using Osmos.Business.Data.NoSql.Entities;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Hubs
{
    public class AppHub : Hub
    {
        public async Task Ping() {
            await Clients.Caller.SendAsync("Pong");
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task NotifyTxn(TxnNotification txnNotification) {
            await Clients.Others.SendAsync("NotifyTxn", txnNotification);
        }

        public async Task GameData(GameData gameData)
        {
            await Clients.Others.SendAsync("GameData", gameData);
        }
    }
}
