using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Osmos.Business.Data.NoSql.Entities;
using Osmos.Core.ApiClient;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth.Extensions;
using Osmos.Workers.Helpers.Eth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Osmos.Workers.Helpers.Eth.BooleanStringIntConverter;

namespace Osmos.Workers.Helpers.Eth
{
    public class EthService
    {
        public async Task<TokenSourceCode> GetSourceCodeAsync(string address)
        {
            var options = new ApiRequestOptions
            {
                Uri = _ethSettings.EtherscanAPIURL + "api?module=contract&action=getsourcecode&address=" + address + "&apikey=" + _ethSettings.EtherscanAPIKEY,
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return null;

            var result = JsonConvert.DeserializeObject<TokenSourceCode>(response.ApiResponse.StringContent);
            return result;
        }

        public async Task<TokenSourceCode> GetSourceCodeSmoothAsync(string address)
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

        public async Task<TokenInfo> GetTokenInfoAsync(string address)
        {
            var options = new ApiRequestOptions
            {
                Uri = "https://api.ethplorer.io/getTokenInfo/" + address + _ethSettings.EthPlorerAPIKEY,
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return null;

            var result = JsonConvert.DeserializeObject<TokenInfo>(response.ApiResponse.StringContent);
            return result;
        }

        public async Task<TokenInfo> GetTokenInfoSmoothAsync(string address)
        {
            try
            {
                return await GetTokenInfoAsync(address);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Holder>> GetTopTokenHoldersAsync(string address, int count)
        {
            var options = new ApiRequestOptions
            {
                Uri = $"https://api.ethplorer.io/getTopTokenHolders/{address}/{_ethSettings.EthPlorerAPIKEY}&limit={count}",
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return null;

            var holderList = JsonConvert.DeserializeObject<HolderList>(response.ApiResponse.StringContent);
            if (holderList == null) return null;
            return holderList.Holders;
        }

        public async Task<List<Holder>> GetTopTokenHoldersSmoothAsync(string address, int count)
        {
            try
            {
                return await GetTopTokenHoldersAsync(address, count);
            }
            catch
            {
                return null;
            }
        }

        public Contract GetContractWithABI(string address, string abi)
        {
            var contract = _web3.Eth.GetContract(abi, address);
            return contract;
        }

        public Contract GetContractWithABISmooth(string address, string abi)
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

        public async Task<string> GetABIAsync(string address)
        {
            var sourceCode = await GetSourceCodeAsync(address);

            if (sourceCode == null || sourceCode.MissingSourceCode()) return _ethSettings.DefautltABI;

            return sourceCode.Result[0].ABI;
        }

        public async Task<Contract> GetContractAsync(string address)
        {
            var abi = await GetABIAsync(address);

            var contract = _web3.Eth.GetContract(abi, address);

            return contract;

        }

        public async Task<List<Txn>> GetPendingTxnsAsync()
        {
            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());

            //Console.WriteLine($"=====> current block {block.Number}");

            var pendingTxns = block.Transactions.Select(t => t.ToTxn()).ToList();
            return pendingTxns;
        }

        public async Task<BlockWithTransactions> GetCurrentBlockAsync()
        {

            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());
            return block;
        }

        public async Task<BlockWithTransactions> GetLatestBlockAsync()
        {

            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());
            return block;
        }

        public async Task<HexBigInteger> GetGasFeesAsync() {

            var gasFees = await _web3.Eth.GasPrice.SendRequestAsync();
            return gasFees;
        }

        public async Task<BlockchainInfos> GetBlockchainInfosAsync()
        {
            BlockWithTransactions current = null;
            BlockWithTransactions previous = null;
            ReceiptsResult receiptsResult = null;
            HexBigInteger gasFees = null;

            await Task.WhenAll(new Task[] {
                Task.Run(async () => {
                    current = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreatePending());
                }),
                Task.Run(async () => {
                    previous = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(BlockParameter.CreateLatest());
                    receiptsResult = await GetReceiptsAsync(previous.Number);
                }),
                Task.Run(async () => {
                    gasFees = await _web3.Eth.GasPrice.SendRequestAsync();
                })
            });

            return new BlockchainInfos
            {
                Current = current,
                Previous = previous,
                Receipts = receiptsResult?.receipts,
                GasFees = (decimal)gasFees.Value
            };
        }

        public async Task<ReceiptsResult> GetReceiptsAsync(HexBigInteger blockNumber)
        {
            string body = "{\"id\":1,\"jsonrpc\":\"2.0\",\"method\":\"alchemy_getTransactionReceipts\",\"params\":[{\"blockNumber\":\"" + blockNumber.HexValue.ToString() + "\"}]}";

            var options = new ApiRequestOptions
            {
                Uri = _ethSettings.AlchemyAPIURL,
                Method = HttpMethod.Post,
                Content = body
            };
            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return null;

            var json = JsonConvert.DeserializeObject<Receipts>(response.ApiResponse.StringContent);
            return json.result;
        }

        public async Task<HexBigInteger> GetLastBlockAsync()
        {
            var lastBlock = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return lastBlock;
        }

        public async Task<Transaction[]> GetBlockTransactionsAsync(HexBigInteger blockNumber)
        {
            var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumber);
            return block.Transactions;
        }

        public async Task<BigInteger> GetWalletBalanceAsync(string contractAddress, string walletAddress)
        {
            var token = _web3.Eth.GetContract(_ethSettings.DefautltABI, contractAddress);
            var balance = await token.GetFunction("balanceOf").CallAsync<BigInteger>(walletAddress);
            return balance;
        }

        public async Task<string> GetTokenNameAsync(Contract contract)
        {
            try
            {
                var nameFunct = contract.GetFunction("name");

                string tokenName = await nameFunct.CallAsync<dynamic>();
                return tokenName;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetTokenSymbolAsync(Contract contract)
        {
            try
            {
                var symbolFunct = contract.GetFunction("symbol");

                string tokenSymbol = await symbolFunct.CallAsync<dynamic>();
                return tokenSymbol;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetTokenOwnerAsync(Contract contract)
        {
            try
            {
                var ownerFunct = contract.GetFunction("owner");

                string tokenOwner = await ownerFunct.CallAsync<dynamic>();
                return tokenOwner;
            }
            catch
            {
                return null;
            }
        }

        public Contract GetFactoryContract(string address = null)
        {
            string _address = address ?? _ethSettings.UniV2Factory;
            var abi = _ethSettings.FactoryAbi;
            var contract = _web3.Eth.GetContract(abi, _address);
            return contract;

        }

        public Task<string> GetPoolQueryAsync(string tokenA, string tokenB, int fee, BlockParameter blockParameter = null)
        {
            var ContractHandler = _web3.Eth.GetContractHandler(_ethSettings.UniV3Factory);
            var getPoolFunction = new GetPoolFunction
            {
                TokenA = tokenA,
                TokenB = tokenB,
                Fee = fee
            };
            return ContractHandler.QueryAsync<GetPoolFunction, string>(getPoolFunction, blockParameter);
        }

        public async Task<string> GetPairV2(string token1, string token2, Contract factory)
        {
            try
            {
                var getPair = factory.GetFunction("getPair");

                string pair = await getPair.CallAsync<dynamic>(token1, token2);

                if (pair == "0x0000000000000000000000000000000000000000") return null;

                return pair;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetPair(string token1, string token2, Contract factory)
        {
            try
            {
                string pairv2 = await GetPairV2(token1, token2, factory);
                string pairv3 = await GetPoolQueryAsync(token1, token2, 3000);
                if (pairv3 == "0x0000000000000000000000000000000000000000")
                    pairv3 = await GetPoolQueryAsync(token1, token2, 10000);
                if (pairv3 != "0x0000000000000000000000000000000000000000") return pairv3;
                else return pairv2;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Pair> GetPairDataAsync(string pairAddress)
        {
            try
            {
                TradingData tradingData;
                var options = new ApiRequestOptions
                {
                    Uri = "https://api.dexscreener.com/latest/dex/pairs/ethereum/" + pairAddress,
                    Method = HttpMethod.Get
                };

                var request = new ApiRequest(options);
                var response = await ApiClient.ExecuteSingleAsync(request);

                if (!response.Succeeded) return null;

                tradingData = JsonConvert.DeserializeObject<TradingData>(response.ApiResponse.StringContent);

                if (tradingData == null || tradingData.Pairs == null || !tradingData.Pairs.Any()) return null;

                var pair = tradingData.Pairs[0];
                return pair;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Pair[]> GetPairDatasAsync(IEnumerable<string> addresses)
        {

            if (!addresses.Any()) return new Pair[] { };

            string addressesStr = string.Join(',', addresses);

            var options = new ApiRequestOptions
            {
                Uri = _ethSettings.DexcreenerAPIUrl + "tokens/" + addressesStr,
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded)
            {
                Console.WriteLine($"can not get pair data for addresses: {addressesStr}");
                return new Pair[] { };
            }

            var result = JsonConvert.DeserializeObject<PairDataList>(response.ApiResponse.StringContent);

            if (result.Pairs == null)
            {
                Console.WriteLine($"can not get pair data for addresses: {addressesStr}");
                return new Pair[] { };
            }

            var pairs = result.Pairs.Where(_ => _.Liquidity != null).ToArray();
            return pairs;
        }

        public async Task<Pair[]> GetPairDatasUnlimitedAsync(IEnumerable<string> addresses)
        {
            var _addresses = addresses.Distinct().ToList();

            var addressSlices = new List<string[]>();
            do
            {
                int size = Math.Min(_addresses.Count, 3);

                var slice = _addresses.Take(size);

                addressSlices.Add(slice.ToArray());

                _addresses.RemoveRange(0, size);

            } while (_addresses.Any());

            var pairs = new List<Pair>();
            int i = 0;
            foreach (var slice in addressSlices)
            {
                //if(i > 0) await Task.Delay(999);

                var _pairs = await GetPairDatasAsync(slice);
                pairs.AddRange(_pairs);

                i++;
            }

            return pairs.ToArray();
        }

        public async Task<TokenSecurity> GetTokenSecurityAsync(string address)
        {

            var options = new ApiRequestOptions
            {
                Uri = $"https://api.gopluslabs.io/api/v1/token_security/1?contract_addresses={address}",
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded)
            {
                Console.WriteLine($"response to security call for {address} failed with code: {response.ApiResponse.StatusCode} because: {response.ApiResponse.StringContent}");
                return null;
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new BooleanStringIntContractResolver()
            };

            try
            {
                var result = JsonConvert.DeserializeObject<TokenSecurityResult>(response.ApiResponse.StringContent, settings);
                if (result == null)
                {
                    Console.WriteLine($"can not deserialize tokenSecurity for address {address}");
                    return null;
                }

                if (result.Code != 1)
                {
                    Console.WriteLine($"response to security call for {address} failed with code {result.Code} because: {result.Message}");
                    return null;
                }

                if (result.Result == null || result.Result.Values == null || !result.Result.Values.Any()) return null;

                return result.Result.Values.First();
            }
            catch (Exception e)
            {
                Console.WriteLine($"can not deserialize tokenSecurity for address {address} because: {e.Message}");
                return null;
            }

        }

        public async Task<double> GetEthPriceAsync()
        {
            var options = new ApiRequestOptions
            {
                Uri = "https://min-api.cryptocompare.com/data/price?fsym=ETH&tsyms=USD",
                Method = HttpMethod.Get
            };

            var request = new ApiRequest(options);
            var response = await ApiClient.ExecuteSingleAsync(request);

            if (!response.Succeeded) return -1;

            var result = JsonConvert.DeserializeObject<EthPrice>(response.ApiResponse.StringContent);
            if (result == null) return -1;

            return result.Usd;
        }

        public EthSettings Settings
        {
            get
            {
                return _ethSettings;
            }
        }

        #region internals

        private readonly EthSettings _ethSettings = null;
        private readonly Web3 _web3 = null;

        public EthService(IOptions<EthSettings> ethSettings)
        {
            _ethSettings = ethSettings.Value;
            _web3 = new Web3(_ethSettings.EthWsURL);
        }

        #endregion
    }

    public class BlockchainInfos
    {
        public BlockWithTransactions Current { get; set; }
        public BlockWithTransactions Previous { get; set; }
        public List<TransactionReceipt> Receipts { get; set; }
        public decimal GasFees { get; set; }
    }

    public class TokenSecurityResult
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public Dictionary<string, TokenSecurity> Result { get; set; }
    }

    public class BooleanStringIntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string str = (string)reader.Value;
                if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                long n = (long)reader.Value;
                if (n == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            throw new JsonSerializationException("Expected boolean string or int value.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((bool)value ? 1 : 0);
        }

        public class BooleanStringIntContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyType == typeof(bool))
                {
                    property.Converter = new BooleanStringIntConverter();
                }

                return property;
            }
        }
    }
}
