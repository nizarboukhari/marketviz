using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Workers.Helpers.Eth.Models
{
    public class EthSettings
    {
        public string EthWsURL { get; set; }
        public string AlchemyAPIURL { get; set; }
        public string EtherscanAPIURL { get; set; }
        public string EtherscanAPIKEY { get; set; }
        public string DexcreenerAPIUrl { get; set; }
        public string DefautltABI { get; set; }
        public string FactoryAbi { get; set; }
        public string UniV2Factory { get; set; }
        public string UniV3Factory { get; set; }
        public string WETH { get; set; }
        public string EthPlorerAPIKEY { get; set; }
    }
}
