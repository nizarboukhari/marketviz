
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Worker.Models;
using System;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    class TradingDataService
    {
        public async static Task<Pair> GetTradingDataAsync(string pairAddress)
        {
            //TODO 
            string jsonTradingData = await Helpers.RequestData("https://api.dexscreener.com/latest/dex/pairs/ethereum/" + pairAddress);

            TradingData tradingData;
            try
            {
                tradingData = JsonConvert.DeserializeObject<TradingData>(jsonTradingData);
            }
            catch (Exception e)
            {
                throw e;
            }

            if (tradingData == null || tradingData.Pairs == null) return null;

            var pair = tradingData.Pairs[0];
            return pair;
        }

        public async static Task<Pair> TryGetTradingDataAsync(string pairAddress)
        {
            try
            {
                return await GetTradingDataAsync(pairAddress);
            }
            catch 
            {
                return null;
            }
        }

    }
}
