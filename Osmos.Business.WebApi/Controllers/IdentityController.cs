using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Osmos.Business.WebApi.Models;
using Osmos.Core.ApiClient;
using Osmos.Workers.Helpers.Eth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/identity")]
    [ApiController]
    public class IdentityController : Controller
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAsync([FromBody] LoginData data) {

            //var result = await _ethService.GetWalletBalanceAsync("0x2C10c0dE3362FF21F8ED6bC7F4AC5e391153fD2c", data.WalletAddress);

            //decimal coins = (decimal)result/ (decimal)1e18;

            //if (coins < 50000) {
            //    return BadRequest($"You need to hold 50k of MARKETVIZ(VIZ) token in order to authenticate, the wallet {data.WalletAddress} only holds {Math.Round(coins, 2)} coins at the moment");
            //}

            string accessToken = await GetAccessTokenAsync();

            return Ok(new {
                accessToken
            });
        }


        [HttpGet("userInfo")]
        public async Task<IActionResult> GetUserInfoAsync([FromHeader] string wallet) {

            if (wallet == null) return Unauthorized();

            var result = await _ethService.GetWalletBalanceAsync("0x2C10c0dE3362FF21F8ED6bC7F4AC5e391153fD2c", wallet);

            decimal coins = (decimal)result / (decimal)1e18;

            return Ok(new
            {
                viz = coins,
                wallet
            });

            //return Ok(new { });
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var client = new HttpClient();

            // discover endpoints from metadata
            var disco = await client.GetDiscoveryDocumentAsync(_appIdentityOptions.Authority);

            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return null;
            }

            // request token
            var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = disco.TokenEndpoint,
                UserName = IdentityServerConfig.Users.First().Username,
                Password = IdentityServerConfig.Users.First().Password,
                ClientId = IdentityServerConfig.Clients.First().ClientId,
                ClientSecret = IdentityServerConfig.ClientSecret,
                Scope = "api"
            });

            if (tokenResponse.IsError)
            {
                //Console.WriteLine(tokenResponse.Error);
                throw new Exception(tokenResponse.Error);
            }

            return tokenResponse.AccessToken;
        }

        #region internals

        private readonly EthService _ethService = null;
        private readonly AppIdentityOptions _appIdentityOptions = null;

        public IdentityController(EthService ethService, IOptions<AppIdentityOptions> appIdentityOptions)
        {
            _ethService = ethService;
            _appIdentityOptions = appIdentityOptions.Value;
        }

        #endregion
    }

    public class LoginData {
        [Required]
        public string WalletAddress { get; set; }
    }
}
