using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/bets")]
    [ApiController]
    public class BetsController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> BetAsync([FromHeader] string wallet, BetData data)
        {
            if (wallet == null) return Unauthorized();

            var options = new QueryOptions<GameData>
            {
                SortOptions = new QuerySortOptions<GameData>
                {
                    Descending = true,
                    Field = _ => _.Id
                },
                Pagination = new QueryPaginationOptions
                {
                    Size = 1,
                    Page = 1
                }
            };

            var gameDataResult = await _gameDataManager.GetManyAsync(options, _ => true);
            var currentBlockNumber = gameDataResult.Entities.FirstOrDefault()?.Id;

            var sameBlockPendingBets = await _betsManager.GetManyAsync(_ => _.Pending && _.SourceBlock == currentBlockNumber && _.Wallet == wallet);
            bool sameBetExists = sameBlockPendingBets.Any(_ => (_.Item != "token" && _.Item == data.Item) || (_.Item == "token" && _.Item == data.Item && _.HotToken?.Address == data.TokenAddress));
            if (sameBetExists)
            {
                return BadRequest("you already have a pending prediction in the same block with the same item");
            }

            HotTokenItem hotToken = null;
            if (data.Item == "token") {
                hotToken = gameDataResult.Entities[0].HotTokens.FirstOrDefault(_ => _.Address == data.TokenAddress);
                if (hotToken == null) return BadRequest("the token is not hot anymore");
            }

            var bet = new Bet {
                Wallet = wallet,
                SourceBlock = currentBlockNumber,
                TargetBlock = (int.Parse(currentBlockNumber) + 3).ToString(),
                Item = data.Item,
                Up = data.Up,
                Pending = true,
                HotToken = hotToken
            };

            var groupBetIds = new List<string> {
                bet.Id
            };

            if (sameBlockPendingBets.Any()) {
                groupBetIds.AddRange(sameBlockPendingBets.Select(_ => _.Id));
            }

            
            bet.GroupBetIds = groupBetIds;

            bet = await _betsManager.CreateOneAsync(bet);

            if (sameBlockPendingBets.Any()) {
                await _betsManager.UpdateManyAsync(sameBlockPendingBets, Builders<Bet>.Update.Set(_ => _.GroupBetIds, groupBetIds));
            }

            var result = new BetEx
            {
                Id = bet.Id,
                Wallet = bet.Wallet,
                CreatedDate = bet.CreatedDate,
                SourceBlock = bet.SourceBlock,
                SourceGameData = gameDataResult.Entities.FirstOrDefault(),
                TargetBlock = bet.TargetBlock,
                //TargetGameData = gameDataResult.Entities.FirstOrDefault(),
                Success = bet.Success,
                SoloSuccess = bet.SoloSuccess,
                Item = bet.Item,
                Pending = bet.Pending,
                HotToken = bet.HotToken,
                Up = bet.Up,
                GroupBetIds = bet.GroupBetIds
            };

            return Ok(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBetsAsync([FromHeader] string wallet) {

            if (wallet == null) return Ok(new GetBetsResult
            {
                Bets = new BetEx[] { },
                Score = 0
            });

            var bets = await _betsManager.GetManyAsync(_ => _.Wallet == wallet);

            var blocks = bets.SelectMany(obj => new[] { obj.TargetBlock, obj.SourceBlock }).Distinct().ToArray();
            var gameDataList = await _gameDataManager.GetManyAsync(_ => blocks.Contains(_.Id));

            var result = bets
                .Select(_ => new BetEx
                {
                    Id = _.Id,
                    Wallet = _.Wallet,
                    CreatedDate = _.CreatedDate,
                    SourceBlock = _.SourceBlock,
                    SourceGameData = gameDataList.FirstOrDefault(_1 => _1.Id == _.SourceBlock),
                    TargetBlock = _.TargetBlock,
                    TargetGameData = gameDataList.FirstOrDefault(_1 => _1.Id == _.TargetBlock),
                    Success = _.Success,
                    SoloSuccess = _.SoloSuccess,
                    Item = _.Item,
                    Pending = _.Pending,
                    HotToken = _.HotToken,
                    Up = _.Up,
                    GroupBetIds = _.GroupBetIds
                })
                .ToArray();

            foreach (var bet in result)
            {
                if (bet.HotToken == null) continue;

                var sourceToken = bet.SourceGameData?.HotTokens.FirstOrDefault(_ => _.Address == bet.HotToken.Address);
                var targetToken = bet.TargetGameData?.HotTokens.FirstOrDefault(_ => _.Address == bet.HotToken.Address);

                if (sourceToken != null)
                {
                    bet.SourcePrice = sourceToken.PriceUsd;
                }

                if (targetToken != null)
                {
                    bet.TargetPrice = targetToken.PriceUsd;
                }
            }

            int score = bets.Where(_ => _.Success != null && (bool)_.Success).Sum(_ => _.GroupBetIds.Count);

            return Ok(new GetBetsResult
            { 
                Bets = result,
                Score = score
            });
        }

        [HttpDelete("{betId}")]
        public async Task<IActionResult> DeleteBetAsync([FromHeader] string wallet, string betId) {

            if (wallet == null) return Unauthorized();

            var options = new QueryOptions<GameData>
            {
                SortOptions = new QuerySortOptions<GameData>
                {
                    Descending = true,
                    Field = _ => _.Id
                },
                Pagination = new QueryPaginationOptions
                {
                    Size = 1,
                    Page = 1
                }
            };

            var gameDataResult = await _gameDataManager.GetManyAsync(options, _ => true);
            var currentBlockNumber = gameDataResult.Entities.FirstOrDefault()?.Id;

            var sameBlockPendingBets = await _betsManager.GetManyAsync(_ => _.Pending && _.SourceBlock == currentBlockNumber && _.Wallet == wallet);

            var bet = sameBlockPendingBets.FirstOrDefault(_ => _.Id == betId);
            if (bet == null) return NotFound();

            var otherBets = sameBlockPendingBets.Where(_ => _.Id != betId).ToArray();
            if (otherBets.Any()) {
                var groupBetIds = otherBets.Select(_ => _.Id).ToList();
                await _betsManager.UpdateManyAsync(otherBets, Builders<Bet>.Update.Set(_ => _.GroupBetIds, groupBetIds));
            }

            await _betsManager.DeleteOneAsync(betId);

            return Ok();
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeaderboardAsync() {

            var bets = await _betsManager.GetManyAsync(_ => _.Success == true);

            var result = bets
                .GroupBy(_ => _.Wallet)
                .Select(_ => new
                {
                    Wallet = _.Key,
                    Score = _.Where(_ => _.Success != null && (bool)_.Success).Sum(_ => _.GroupBetIds.Count)
                })
                .OrderByDescending(_ => _.Score)
                .Take(12)
                .ToArray();

            return Ok(result);
        }

        #region internals

        private readonly GameDataManager _gameDataManager = null;
        private readonly BetsManager _betsManager = null;
        //private readonly HotTokensManager _hotTokensManager = null;

        public BetsController(GameDataManager gameDataManager, BetsManager betsManager, HotTokensManager hotTokensManager)
        {
            _gameDataManager = gameDataManager;
            _betsManager = betsManager;
            //_hotTokensManager = hotTokensManager;
        }

        public class BetData {
            [Required]
            public string Item { get; set; }
            public bool Up { get; set; }
            public string TokenAddress { get; set; }
        }

        public class BetEx : Bet {
            public GameData TargetGameData { get; set; }
            public string TargetPrice { get; set; }
            public GameData SourceGameData { get; set; }
            public string SourcePrice { get; set; }
        }

        public class GetBetsResult {
            public BetEx[] Bets { get; set; }
            public int Score { get; set; }
        }

        #endregion
    }
}
