using Osmos.Business.Data.NoSql.Managers;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.NewTokens
{
    internal class NewTokensApp : BaseApp
    {
        protected override async Task RunAsync()
        {
            var functions = new Func<Task>[] { CreationTxnsLoop };
            await ActionsHelper.LoopAsync(functions, 6000);
        }

        private async Task CreationTxnsAsync()
        {
            await ActionsHelper.LoopAsync(CreationTxnsLoop, 1000);
        }

        private async Task CreationTxnsLoop() {
            var txns = await _txnsManager.GetManyAsync(_ => _.FunctionSignature == "60806040");

           
        }

        #region internals

        private readonly TxnsManager _txnsManager = null;
        private readonly TokensManager _tokensManager = null;
        private readonly IgnoredTokensManager _ignoredTokensManager = null;
        private readonly EthService _ethService = null;

        public NewTokensApp(TxnsManager txnsManager, TokensManager tokensManager, IgnoredTokensManager ignoredTokensManager, EthService ethService)
        {
            _txnsManager = txnsManager;
            _tokensManager = tokensManager;
            _ignoredTokensManager = ignoredTokensManager;
            _ethService = ethService;
        }

        #endregion
    }
}
