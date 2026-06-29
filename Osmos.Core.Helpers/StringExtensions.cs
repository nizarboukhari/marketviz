using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Osmos.Core.Helpers
{
    public static class StringExtensions
    {
        public static string Despace(this string longString)
        {
            var sb = new StringBuilder();
            bool lastWasSpace = true;

            for (int i = 0; i < longString.Length; i++)
            {
                if (char.IsWhiteSpace(longString[i]) && lastWasSpace)
                {
                    continue;
                }

                lastWasSpace = char.IsWhiteSpace(longString[i]);

                sb.Append(longString[i]);
            }

            if (char.IsWhiteSpace(sb[sb.Length - 1]))
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        public static string Truncate(this string input, int maxLength)
        {
            if (input.Length > maxLength)
                return input.Substring(0, maxLength);
            return input;
        }

        public static string ToAlphaNumeric(this string input, params char[] allowedCharacters)
        {
            return new string(Array.FindAll(input.ToCharArray(), c => char.IsLetterOrDigit(c) || allowedCharacters.ToList().Contains(c)));
        }

        public static string RemoveExtraSpace(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            int current = 0;
            char[] output = new char[input.Length];
            bool skipped = false;

            foreach (char c in input.ToCharArray())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!skipped)
                    {
                        if (current > 0)
                            output[current++] = ' ';

                        skipped = true;
                    }
                }
                else
                {
                    skipped = false;
                    output[current++] = c;
                }
            }

            return new string(output, 0, current);
        }
    }
}
