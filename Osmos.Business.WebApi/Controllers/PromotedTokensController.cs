using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osmos.Business.Data.NoSql.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/promoted-tokens")]
    [ApiController]
    public class PromotedTokensController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPromotedTokensAsync() {
            
            var promotedTokens = await _promotedTokensManager.GetManyAsync();
            var promotedTokenIds = promotedTokens.Select(_ => _.Id).ToArray();

            var tokens = await _tokensManager.GetManyAsync(_ => promotedTokenIds.Contains(_.Address));

            return Ok(tokens);
        }

        #region internals

        private readonly TokensManager _tokensManager = null;
        private readonly PromotedTokensManager _promotedTokensManager = null;
        public PromotedTokensController(TokensManager tokensManager, PromotedTokensManager promotedTokensManager)
        {
            _tokensManager = tokensManager;
            _promotedTokensManager = promotedTokensManager;
        }

        #endregion

    }
}
