using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osmos.Business.Data.NoSql.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/hot-tokens")]
    [ApiController]
    public class HotTokensController : Controller
    {

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetHotTokensAsync() {

            var hotTokens = await _hotTokensManager.GetManyAsync();

            bool isAuthenticated = User.Identity.IsAuthenticated;

            var result = hotTokens.OrderByDescending(_ => _.Score).ToArray();
            return Ok(result);
        }

        [HttpGet("{tokenAddress}/score-history")]
        [AllowAnonymous]
        public async Task<IActionResult> GetScoreHistoryAsync(string tokenAddress)
        {

            var hotTokenResult = await _hotTokensManager.GetOneAsync(_ => _.Token.Address == tokenAddress);
            if (hotTokenResult == null) return NotFound();

            var scoreHistory = new List<object[]>();
            foreach (var item in hotTokenResult.ScoreDates)
            {
                var point = new object[] {
                    item.Date.ToTimeStamp(),
                    item.Score
                };

                scoreHistory.Add(point);
            }

            var priceHistory = new List<object[]>();
            foreach (var item in hotTokenResult.Token.Prices.Where(_ => _.Value != null))
            {
                var point = new object[] {
                    item.Date.ToTimeStamp(),
                    item.Value
                };

                priceHistory.Add(point);
            }

            return Ok(new
            {
                scoreHistory,
                priceHistory
            });
        }

        #region internals

        private readonly HotTokensManager _hotTokensManager = null;

        public HotTokensController(HotTokensManager hotTokensManager)
        {
            _hotTokensManager = hotTokensManager;
        }

        #endregion
    }

    public static class DateExtensions
    {
        public static long ToTimeStamp(this DateTime value)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan elapsedTime = value - dateTime;
            return (long)elapsedTime.TotalMilliseconds;
        }
    }
}
