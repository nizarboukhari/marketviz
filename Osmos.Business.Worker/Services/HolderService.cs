using Nethereum.Contracts.Standards.ERC20.TokenList;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Worker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    public class HolderService
    {
        public static async Task<List<Holding>> GetHolderERC20Assets(string holderAddress)
        {
			var web3 = new Web3(Settings.EthWsURL);

			//TODO add our tokens
			// + get all the tokens then for each, make sure its not in lists, then create ERC20 object and add to list 

			//using the default uniswap token 
			var Maintokens = await new TokenListService().LoadFromUrl(TokenListSources.UNISWAP);

			var tokensOwned = await web3.Eth.ERC20.GetAllTokenBalancesUsingMultiCallAsync(
					new string[] { holderAddress }, Maintokens.Where(x => x.ChainId == 1),
					BlockParameter.CreateLatest());

			//Filtering only the tokens from the token list that have a positive balance
			var tokensWithBalance = tokensOwned.Where(x => x.GetTotalBalance() > 0);

			List<Holding> holdings = new List<Holding>();

			foreach (var tokenWithBalance in tokensWithBalance)
			{
				var balance = tokenWithBalance.OwnersBalances.FirstOrDefault(x => x.Owner.IsTheSameAddress(holderAddress)).Balance;

				holdings.Add(
					new Holding
					{
						Address = tokenWithBalance.Token.Address,
						Value = Nethereum.Util.UnitConversion.Convert.FromWei(balance, tokenWithBalance.Token.Decimals)
					});
			}

			return holdings;
		}

		public static Task<List<Holding>> GetHolderERC721Assets(string holderAddress) {
			return null;		
		}

	}
}
