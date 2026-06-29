using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Worker.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    class TokenInfoService
    {
        public async static Task<TokenInfo> GetBlockChainData(string tokenAddress)
        {
            //TODO 
            string jsonTradingData = await Helpers.RequestData("https://api.ethplorer.io/getTokenInfo/" + tokenAddress + Settings.EthPlorerAPIKEY);

            try
            {
                var tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(jsonTradingData);
                return tokenInfo;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async static Task<TokenInfo> TryGetBlockChainData(string tokenAddress)
        {
            try
            {
                return await GetBlockChainData(tokenAddress);
            }
            catch
            {
                return null;
            }
        }

        public async static Task<List<Holder>> GetTopTokenHolders(string tokenAddress, int count)
        {
            string url = $"https://api.ethplorer.io/getTopTokenHolders/{tokenAddress}/{Settings.EthPlorerAPIKEY}&limit={count}";
            //TODO 
            string jsonTradingData = await 
                Helpers.RequestData(url);

            try
            {
                var holderList = JsonConvert.DeserializeObject<HolderList>(jsonTradingData);
                if (holderList == null) return null;

                return holderList.Holders;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async static Task<List<Holder>> TryGetTopTokenHolders(string tokenAddress, int count)
        {
            try
            {
                return await GetTopTokenHolders(tokenAddress, count);
            }
            catch
            {
                return null;
            }
        }
    }
}
