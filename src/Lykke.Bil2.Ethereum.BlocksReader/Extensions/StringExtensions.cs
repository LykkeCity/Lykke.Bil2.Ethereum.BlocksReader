using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Bil2.Ethereum.BlocksReader.Extensions
{
    public static class StringExtensions
    {
        public static byte[] ToBytes(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            return bytes;
        }
    }
}
