using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/game-data")]
    [ApiController]
    public class GameDataController : Controller
    {
        [HttpGet("lastest")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLatestAsync()
        {
            var options = new QueryOptions<GameData> {
                SortOptions = new QuerySortOptions<GameData> { 
                    Descending = true,
                    Field = _ => _.Id
                },
                Pagination = new QueryPaginationOptions { 
                    Size = 12,
                    Page = 1
                }
            };

            var gameDataResult = await _gameDataManager.GetManyAsync(options, _ => true);

            return Ok(gameDataResult.Entities);
        }

        #region internals

        private readonly GameDataManager _gameDataManager = null;

        public GameDataController(GameDataManager gameDataManager)
        {
            _gameDataManager = gameDataManager;
        }

        #endregion
    }
}
