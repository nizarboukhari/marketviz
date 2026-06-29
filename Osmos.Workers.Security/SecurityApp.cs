using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.Security
{
    internal class SecurityApp: BaseApp
    {
        protected override async Task RunAsync()
        {
            //var balance = await _ethService.GetWalletBalanceAsync("0xE1f4aCD636D653Fec7ab4E838422CB49711416e1", "0xe3b9a62fbb750fd3e94cca17430087a8bdad3fce");
            //return;
            var functions = new Func<Task>[] { SecurityAsync, RequestsCountLogAsync };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private async Task SecurityLoop() {
            var now = DateTime.UtcNow;

            var hotTokenResult = await _hotTokensManager.GetManyAsync();

            var toUpdate = new List<Token>();
            foreach (var item in hotTokenResult)
            {
                var tokenSecurity = await _ethService.GetTokenSecurityAsync(item.Token.Address);
                lock (_requestsCountLock) {
                    _requestsCount++;
                }
                if (tokenSecurity == null)
                {
                    continue;
                }
                item.Token.Security = tokenSecurity;
                toUpdate.Add(item.Token);
            }

            await _tokensManager.BlukUpdateManySecurityAsync(toUpdate);

            string addressesStr = string.Join(',', toUpdate.Select(_ => _.Address));

            Console.WriteLine($"updated security for {toUpdate.Count} token in {(DateTime.UtcNow - now).TotalSeconds} seconds");
        }

        private async Task SecurityAsync()
        {
            await ActionsHelper.LoopAsync(SecurityLoop, 222);
        }

        private int _requestsCount = 0;
        private readonly object _requestsCountLock = new object();
        private async Task RequestsCountLogAsync() {
            await ActionsHelper.LoopAsync(async () => {

                await Task.Delay(60000);

                lock (_requestsCountLock)
                {
                    Console.WriteLine($"made {_requestsCount} calls/min");
                    _requestsCount = 0;
                }
            });
        }

        #region internals

        private readonly TokensManager _tokensManager = null;
        private readonly HotTokensManager _hotTokensManager = null;
        private readonly EthService _ethService = null;

        public SecurityApp(TokensManager tokensManager, HotTokensManager hotTokensManager, EthService ethService)
        {
            _tokensManager = tokensManager;
            _hotTokensManager = hotTokensManager;
            _ethService = ethService;
        }

        #endregion
    }
}
