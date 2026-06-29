using LinqKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.WebApi.Models;
using Osmos.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Business.WebApi.Controllers
{
    [Route("api/txns")]
    [ApiController]
    //[AllowAnonymous]
    public class TxnsController : Controller
    {
        [HttpGet("by-block")]
        public async Task<IActionResult> GetTxnsByBlockAsync() {

            var response = await _txnsManager.GroupTxnsByBlockAsync();

            var result = response.Select(g => new { 
                g.Id,
                Total = g.Txns.Count(),
                Success = g.Txns.Count(t => t.Receipt != null && t.Receipt.Succeeded),
                Fail = g.Txns.Count(t => t.Receipt != null && !t.Receipt.Succeeded),
                Pending = g.Txns.Count(t => t.Receipt == null),
                PendingTxns = g.Txns.Where(t => t.Receipt == null)

            }).ToArray();

            return Ok(result);
        }

        [HttpGet("by-token")]
        public async Task<IActionResult> GetTxnsByTokenAsync()
        {
            var result = await _txnsManager.GroupByTokenAsync();

            var addresses = result.Select(_ => _.Id).ToArray();

            var tokens = await _tokensManager.GetManyAsync(t => addresses.Contains(t.Address));

            foreach (var item in result)
            {
                var token = tokens.FirstOrDefault(t => t.Address == item.Id);
                if (token == null) continue;

                item.Name = token.Name;
                item.Symbol = token.Symbol;
                item.Owner = token.Owner;
            }

            return Ok(result);
        }

        [HttpGet("token/{address}")]
        public async Task<IActionResult> GetTokenTxnsAsync(string address, [FromQuery] int limit = 12) {

            var options = new QueryOptions<Txn> { 
                SortOptions = new QuerySortOptions<Txn> { 
                    Descending = true,
                    Field = t => t.BlockNumber
                },
                Pagination = new QueryPaginationOptions { 
                    Page = 1,
                    Size = limit
                }
            };

            string _address = address.ToLower();

            var token = await _tokensManager.GetOneAsync(t => t.Address == _address);

            string _addressNoPrefix = _address.Replace("0x", "");

            var response = await _txnsManager.GetManyAsync(options, t => t.To == _address || (t.Receipt != null && t.Receipt.ContractAddress == _address) || t.Input.Contains(_addressNoPrefix));

            var txns = response.Entities.Select(t => new TxnEx(t, _address)).ToArray();

            if (token != null) {
                foreach (var txn in txns)
                {
                    txn.FromOwner = (token.Owner != null && txn.From.ToLower() == token.Owner.ToLower()) || 
                        (token.TokenInfo != null && token.TokenInfo.Owner != null && txn.From.ToLower() == token.TokenInfo.Owner.ToLower());

                    if (txn.FunctionName != null && txn.FunctionName.ToLower().StartsWith("swap") && txn.FunctionName.ToLower().Contains("eth") && txn.FunctionName.ToLower().Contains("tokens")) {
                        int indexETH = txn.FunctionName.ToLower().IndexOf("eth");
                        int indexTokens = txn.FunctionName.ToLower().IndexOf("tokens");

                        txn.Type = indexETH < indexTokens ? TxnExType.Buy : TxnExType.Sell;
                    }

                    if (txn.Type == TxnExType.None) {
                        if (txn.Input != null && txn.Input.ToLower().Contains(_addressNoPrefix) && txn.Input.ToLower().Contains("c02aaa39b223fe8d0a0e5c4f27ead9083c756cc2")) {
                            int indexToken = txn.Input.ToLower().LastIndexOf(_addressNoPrefix);
                            int indexEth = txn.Input.ToLower().LastIndexOf("c02aaa39b223fe8d0a0e5c4f27ead9083c756cc2");

                            txn.Type = indexEth < indexToken ? TxnExType.Buy : TxnExType.Sell;
                        }
                    }
                }
            }

            return Ok(txns);
        }

        [HttpGet("tokens")]
        public async Task<IActionResult> GetTokensTxnsAsync([FromQuery] string[] addresses, [FromQuery] int limit = 12)
        {

            var options = new QueryOptions<Txn>
            {
                SortOptions = new QuerySortOptions<Txn>
                {
                    Descending = true,
                    Field = t => t.BlockNumber
                },
                Pagination = new QueryPaginationOptions
                {
                    Page = 1,
                    Size = limit
                }
            };

            string[] _addresses = addresses.Select(_ => _.ToLower()).ToArray();

            var tokens = await _tokensManager.GetManyAsync(t => _addresses.Contains(t.Address));

            string[] _addressesNoPrefix = _addresses.Select(_ => _.Replace("0x", "")).ToArray();

            var predicate = PredicateBuilder.New<Txn>();

            predicate.Or(t => _addresses.Contains(t.To));
            predicate.Or(t => t.Receipt != null && _addresses.Contains(t.Receipt.ContractAddress));
            foreach (var _addressNoPrefix in _addressesNoPrefix)
            {
                predicate.Or(t => t.Input.Contains(_addressNoPrefix));
            }

            var response = await _txnsManager.GetManyAsync(options, predicate);

            var txns = response.Entities.Select(t => new TxnEx(t)).ToArray();

            foreach (var txn in txns)
            {
                var _address = _addresses.FirstOrDefault(_ => _ == txn.To);
                if (_address != null) {
                    txn.TokenAddress = _address;
                    continue;
                }

                if (txn.Receipt != null && _addresses.Contains(txn.Receipt.ContractAddress)) {
                    _address = _addresses.FirstOrDefault(_ => _ == txn.Receipt.ContractAddress);
                    if (_address != null) {
                        txn.TokenAddress = _address;
                        continue;
                    }
                }

                foreach (var _ in _addresses)
                {
                    string _addressNoPrefix = _.Replace("0x", "");
                    if (txn.Input.Contains(_addressNoPrefix)) {
                        txn.TokenAddress = _;
                        break;
                    }
                }
            }

            foreach (var token in tokens)
            {
                if (token == null) continue;

                foreach (var txn in txns)
                {
                    txn.FromOwner = (token.Owner != null && txn.From.ToLower() == token.Owner.ToLower()) ||
                        (token.TokenInfo != null && token.TokenInfo.Owner != null && txn.From.ToLower() == token.TokenInfo.Owner.ToLower());

                    if (txn.FunctionName != null && txn.FunctionName.ToLower().StartsWith("swap") && txn.FunctionName.ToLower().Contains("eth") && txn.FunctionName.ToLower().Contains("tokens"))
                    {
                        int indexETH = txn.FunctionName.ToLower().IndexOf("eth");
                        int indexTokens = txn.FunctionName.ToLower().IndexOf("tokens");

                        txn.Type = indexETH < indexTokens ? TxnExType.Buy : TxnExType.Sell;
                    }

                    if (txn.Type == TxnExType.None && txn.TokenAddress != null)
                    {
                        string _addressNoPrefix = txn.TokenAddress.Replace("0x", "");

                        if (txn.Input != null && txn.Input.ToLower().Contains(_addressNoPrefix) && txn.Input.ToLower().Contains("c02aaa39b223fe8d0a0e5c4f27ead9083c756cc2"))
                        {
                            int indexToken = txn.Input.ToLower().LastIndexOf(_addressNoPrefix);
                            int indexEth = txn.Input.ToLower().LastIndexOf("c02aaa39b223fe8d0a0e5c4f27ead9083c756cc2");

                            txn.Type = indexEth < indexToken ? TxnExType.Buy : TxnExType.Sell;
                        }
                    }
                }
            }

            return Ok(txns);
        }

        [HttpGet("owner/{address}")]
        public async Task<IActionResult> GetOwnerTxnsAsync(string address, [FromQuery] int limit = 12) {

            var options = new QueryOptions<Txn>
            {
                SortOptions = new QuerySortOptions<Txn>
                {
                    Descending = true,
                    Field = t => t.BlockNumber
                },
                Pagination = new QueryPaginationOptions
                {
                    Page = 1,
                    Size = limit
                }
            };

            string _address = address.ToLower();

            var response = await _txnsManager.GetManyAsync(options, t => t.From == _address || (t.Receipt != null && t.Receipt.From == _address));

            return Ok(response.Entities);
        }

        #region internals

        private readonly TxnsManager _txnsManager = null;
        private readonly TokensManager _tokensManager = null;

        public TxnsController(TxnsManager txnsManager, TokensManager tokensManager)
        {
            _txnsManager = txnsManager;
            _tokensManager = tokensManager;
        }

        #endregion 
    }
}
