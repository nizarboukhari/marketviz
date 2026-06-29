using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osmos.Business.Data.NoSql.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/txn-notifications")]
    [ApiController]
    public class TxnNotificationsController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetTxnNotificationsAsync() {

            var notifications = await _txnNotificationsManager.GetManyAsync();
            return Ok(notifications);
        }

        #region internals

        private readonly TxnNotificationsManager _txnNotificationsManager = null;

        public TxnNotificationsController(TxnNotificationsManager txnNotificationsManager)
        {
            _txnNotificationsManager = txnNotificationsManager;
        }

        #endregion
    }
}
