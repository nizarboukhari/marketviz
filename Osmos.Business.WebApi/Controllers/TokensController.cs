using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/tokens")]
    [ApiController]
    public class TokensController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetTokensAsync() {

            var tokens = await _tokensManager.GetManyAsync();
            tokens = tokens
                //.Where(t => t.LastUpdatedDate < DateTime.UtcNow.AddSeconds(-5))
                .Where(t => t.TokenInfo != null && t.TokenInfo.HoldersCount != 0)
                .OrderByDescending(t => t.LastUpdatedDate)
                //.Take(150)
                .ToArray();

            return Ok(tokens);
        }

        [HttpGet("{address}")]
        //[AllowAnonymous]
        public async Task<IActionResult> GetTokenAsync(string address) {
            var token = await _tokensManager.GetOneAsync(t => t.Address == address);
            if (token == null) return NotFound();

            return Ok(token);
        }

        [HttpGet("opportunity")]
        [AllowAnonymous]
        public async Task<IActionResult> OpportunityAsync() {

            var oppResult = await _telegramOpportunityMessagesManager.GetTopOpportunityAsync();

            var tokenAddresses = oppResult.Select(_ => _.TokenAddress).Distinct().ToArray();
            var tokens = await _tokensManager.GetManyAsync(_ => tokenAddresses.Contains(_.Address));

            var result = new List<OpportunityResultEx>(); 
            foreach (var item in oppResult)
            {
                var token = tokens.FirstOrDefault(_ => _.Address == item.TokenAddress);
                if (token == null || token.Pairs == null || !token.Pairs.Any() || token.Pairs[0].Fdv < 10) continue;

                result.Add(new OpportunityResultEx(item, token));
                if (result.Count == 12) break;
            }

            return Ok(result);
        }

        [HttpGet("score")]
        [AllowAnonymous]
        public async Task<IActionResult> ScoreAsync() {

            var options = new QueryOptions<TelegramScoreMessage> { 
                SortOptions = new QuerySortOptions<TelegramScoreMessage> { 
                    Descending = true,
                    Field = _ => _.Score
                },
                Pagination = new QueryPaginationOptions { 
                    Page = 1,
                    Size = 12
                }
            };

            var now = DateTime.UtcNow;
            var time = now.AddDays(-1);

            var scoreResult = await _telegramScoreMessagesManager.GetManyAsync(options, _ => _.UpdatedDate > time);

            var tokenAddresses = scoreResult.Entities.Select(_ => _.TokenAddress).Distinct().ToArray();
            var tokens = await _tokensManager.GetManyAsync(_ => tokenAddresses.Contains(_.Address));

            var result = new List<ScoreResult>();
            foreach (var item in scoreResult.Entities)
            {
                var token = tokens.FirstOrDefault(_ => _.Address == item.TokenAddress);
                if (token == null || token.Pairs == null || !token.Pairs.Any() || token.Pairs[0].Fdv < 10) continue;

                result.Add(new ScoreResult(item, token));
                if (result.Count == 12) break;
            }

            return Ok(result);
        }

        //[Obsolete]
        //[HttpGet("trending")]
        //[AllowAnonymous]
        //public async Task<IActionResult> GetHotTokensAsync()
        //{

        //    var result = await _tokensManager.GetHotTokensAsync();
        //    return Ok(result);
        //}

        [HttpGet("dummy")]
        public async Task<IActionResult> DummyAsync() {

            var signer = new EthereumMessageSigner();

            string message = "{\"domain\":{\"chainId\":1,\"name\":\"Ether Mail\",\"verifyingContract\":\"0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC\",\"version\":\"1\"},\"message\":{\"contents\":\"Hello, Bob!\",\"attachedMoneyInEth\":4.2,\"from\":{\"name\":\"Cow\",\"wallets\":[\"0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826\",\"0xDeaDbeefdEAdbeefdEadbEEFdeadbeEFdEaDbeeF\"]},\"to\":[{\"name\":\"Bob\",\"wallets\":[\"0xbBbBBBBbbBBBbbbBbbBbbbbBBbBbbbbBbBbbBBbB\",\"0xB0BdaBea57B0BDABeA57b0bdABEA57b0BDabEa57\",\"0xB0B0b0b0b0b0B000000000000000000000000000\"]}]},\"primaryType\":\"Mail\",\"types\":{\"EIP712Domain\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"version\",\"type\":\"string\"},{\"name\":\"chainId\",\"type\":\"uint256\"},{\"name\":\"verifyingContract\",\"type\":\"address\"}],\"Group\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"members\",\"type\":\"Person[]\"}],\"Mail\":[{\"name\":\"from\",\"type\":\"Person\"},{\"name\":\"to\",\"type\":\"Person[]\"},{\"name\":\"contents\",\"type\":\"string\"}],\"Person\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"wallets\",\"type\":\"address[]\"}]}}";
            string signature = "0xafb86a0781f5b24cc53120e907623e862c6d593954907afe6a42bd025c33b6fb1bae74d12f8f9f2986dcbfb6e434e634a3ff216ed149f1023e393085492ae2d11b";

            var dataSigner = new Eip712TypedDataSigner();

            var bytes = Encoding.UTF8.GetBytes(message);

            string result = dataSigner.RecoverFromSignatureV4(bytes, signature);

            string address = signer.EcRecover(bytes, signature);

            var util = new AddressUtil();
            string sent = util.ConvertToChecksumAddress("0x5daa78488c90059607181f188970072328a18c3b");
            string recovered = util.ConvertToChecksumAddress(address);

            return Ok(new { sent , recovered});
        }

        #region internals

        private readonly TokensManager _tokensManager = null;
        private readonly TelegramOpportunityMessagesManager _telegramOpportunityMessagesManager = null;
        private readonly TelegramScoreMessagesManager _telegramScoreMessagesManager = null;

        public TokensController(TokensManager tokensManager, TelegramOpportunityMessagesManager telegramOpportunityMessagesManager, TelegramScoreMessagesManager telegramScoreMessagesManager)
        {
            _tokensManager = tokensManager;
            _telegramOpportunityMessagesManager = telegramOpportunityMessagesManager;
            _telegramScoreMessagesManager = telegramScoreMessagesManager;
        }

        #endregion

        public class OpportunityResultEx: OpportunityResult
        {
            public Token Token { get; set; }

            public OpportunityResultEx(OpportunityResult opportunityResult, Token token)
            {
                Id = opportunityResult.Id;
                TokenAddress = opportunityResult.TokenAddress;
                MinPrice = opportunityResult.MinPrice;
                MaxPrice = opportunityResult.MaxPrice;
                Percentage = opportunityResult.Percentage;
                Token = token;
            }
        }

        public class ScoreResult {
            [BsonId]
            public string Id { get; set; }
            public string TokenAddress { get; set; }
            public double Score { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
            public Token Token { get; set; }

            public ScoreResult(TelegramScoreMessage telegramScoreMessage, Token token)
            {
                Id = telegramScoreMessage.Id;
                TokenAddress = telegramScoreMessage.TokenAddress;
                Score = telegramScoreMessage.Score;
                CreatedDate = telegramScoreMessage.CreatedDate;
                UpdatedDate = telegramScoreMessage.UpdatedDate;
                Token = token;
            }
        }

    }
}
