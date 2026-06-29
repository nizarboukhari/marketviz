using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using Twilio.TwiML.Fax;

namespace Osmos.Business.Worker.Models
{
    public class ReceiptsResult
    {
        public List<TransactionReceipt> receipts { get; set; }
    }

    public class Receipts
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public ReceiptsResult result { get; set; }
    }

}
