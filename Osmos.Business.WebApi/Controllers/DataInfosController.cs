using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osmos.Business.Data.NoSql.Managers;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/data-infos")]
    [ApiController]
    public class DataInfosController : Controller
    {
        [HttpGet]
        //[AllowAnonymous]
        public async Task<IActionResult> GetDataInfosAsync()
        {
            var dataInfos = await _dataInfosManager.GetOneAsync();
            return Ok(dataInfos);
        }

        #region internals

        private readonly DataInfosManager _dataInfosManager = null;

        public DataInfosController(DataInfosManager dataInfosManager)
        {
            _dataInfosManager = dataInfosManager;
        }

        #endregion
    }
}
