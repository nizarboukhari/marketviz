using Nethereum.Contracts;
using Nethereum.RPC;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Business.Worker.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Osmos.Business.Worker.Services
{
    class TokenService
    {
        public async static Task<string> GetTokenNameAsync(Contract contract)
        {
            try
            {
                var nameFunct = contract.GetFunction("name");

                var tokenName = await nameFunct.CallAsync<dynamic>();
                return tokenName;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async static Task<string> TryGetTokenNameAsync(Contract contract)
        {
            try
            {
                return await GetTokenNameAsync(contract);
            }
            catch
            {
                return null;
            }
        }

        public async static Task<string> GetTokenSymbolAsync(Contract contract)
        {
            var symbolFunct = contract.GetFunction("symbol");

            var tokenSymbol = await symbolFunct.CallAsync<dynamic>();
            return tokenSymbol;
        }

        public async static Task<string> TryGetTokenSymbolAsync(Contract contract)
        {
            try
            {
                return await GetTokenSymbolAsync(contract);
            }
            catch 
            {
                return null;
            }
        }

        public async static Task<string> GetTokenOwnerAsync(Contract contract)
        {            
            var ownerFunct = contract.GetFunction("owner");

            var tokenOwner = await ownerFunct.CallAsync<dynamic>();
            return tokenOwner;
        }

        public async static Task<string> TryGetTokenOwnerAsync(Contract contract)
        {
            try
            {
                return await GetTokenOwnerAsync(contract);
            }
            catch
            {
                return null;
            }
        }

        public async static Task<string> GetTokenNameFromAddressAsync(string address)
        {
            var contract = await GetContractAsync (address);
            var nameFunct = contract.GetFunction("name");

            var tokenName = await nameFunct.CallAsync<dynamic>();
            return tokenName;
        }

        public async static Task<string> GetTokenSymbolFromAddressAsync(string address)
        {
            var contract = await GetContractAsync (address);
            var symbolFunct = contract.GetFunction("symbol");

            var tokenSymbol = await symbolFunct.CallAsync<dynamic>();
            return tokenSymbol;
        }

        public async static Task<string> GetTokenOwnerFromAddressAsync(string address)
        {
            var contract = await GetContractAsync(address);
            var ownerFunct = contract.GetFunction("owner");

            var tokenSymbol = await ownerFunct.CallAsync<dynamic>();
            return tokenSymbol;
        }

        public static async Task<string> GetPair(string token1, string token2, Contract factory)
        {
            var getPair = factory.GetFunction("getPair");

            string pair = await getPair.CallAsync<dynamic>(token1, token2);

            if (pair == "0x0000000000000000000000000000000000000000") return null;

            return pair;
        }

        public static async Task<string> TryGetPair(string token1, string token2, Contract factory)
        {
            try
            {
                return await GetPair(token1, token2, factory);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> GetABIAsync(string address)
        {
            var sourceCode = await GetSourceCodeAsync(address);

            if (sourceCode == null || sourceCode.MissingSourceCode()) return Settings.defautltABI;

            return sourceCode.Result[0].ABI;
        }

        public static async Task<TokenSourceCode> GetSourceCodeAsync(string address)
        {
            string url = Settings.etherscanAPIURL + "api?module=contract&action=getsourcecode&address=" + address + "&apikey=" + Settings.etherscanAPIKEY;
            string jsonSourceCode = await Helpers.RequestData(url);

                var sourceCode = JsonConvert.DeserializeObject<TokenSourceCode>(jsonSourceCode);

                return sourceCode;            
        }

        public static async Task<TokenSourceCode> TryGetSourceCodeAsync(string address)
        {
            try
            {
                return await GetSourceCodeAsync(address);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<Contract> GetContractAsync(string address)
        {
            var abi = await GetABIAsync(address);

            var web3 = new Web3(Settings.EthWsURL);

            var contract = web3.Eth.GetContract(abi, address);

            return contract;

        }

        public static async Task<Contract> TryGetContractAsync(string address)
        {
            try
            {
                return await GetContractAsync(address);
            }
            catch
            {
                return null;
            }
        }

        public static Contract GetContractWithABI(string address, string abi)
        {
            var web3 = new Web3(Settings.EthWsURL);

            var contract = web3.Eth.GetContract(abi, address);

            return contract;
        }

        public static Contract TryGetContractWithABI(string address, string abi)
        {
            try
            {
                return GetContractWithABI(address, abi);
            }
            catch
            {
                return null;                
            }
        }

        public static Contract GetContractFromAddress()
        {
            var web3 = new Web3(Settings.EthWsURL);

            var service = new EthApiService(web3.Client);

            var contract = new Contract(service, Settings.defautltABI, "0xE711b5e7ae701bcF9A0CE55a9FC2534DdCad0f23");

            return contract;
        }

        public static Contract GetFactoryContract(string address)
        {
            var abi = Settings.factoryAbi;
            var web3 = new Web3(Settings.EthWsURL);

            var contract = web3.Eth.GetContract(abi, address);
            return contract;

        }

        public static Contract TryGetFactoryContract(string address)
        {
            try
            {
                return GetFactoryContract(address);
            }
            catch
            {
                return null;
            }

        }

        public static Contract GetRouterContract(string address)
        {
            var abi = Settings.routerAbi;
            var web3 = new Web3(Settings.EthWsURL);

            var contract = web3.Eth.GetContract(abi, address);
            return contract;
        }

        public static async Task<BigDecimal> GetBalanceAsync(string address) {

            var web3 = new Web3(Settings.EthWsURL);

            var balanceInWei = await web3.Eth.GetBalance.SendRequestAsync(address);
            BigDecimal balanceInEth = Web3.Convert.FromWei(balanceInWei.Value);

            return balanceInEth;
        }
    }
}
