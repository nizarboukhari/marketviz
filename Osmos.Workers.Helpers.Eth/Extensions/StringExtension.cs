using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Workers.Helpers.Eth.Extensions
{
    public static class StringExtension
    {
        public static string ToFunctionName(this string input)
        {
            string name = input.Replace("0x", "");
            name = name.Substring(0, Math.Min(8, name.Length));
            return name;
        }
    }
}
