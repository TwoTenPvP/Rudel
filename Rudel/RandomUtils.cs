using System;
using System.Security.Cryptography;

namespace Rudel
{
    internal static class RandomUtils
    {
        private static readonly byte[] buffer = new byte[32];
        private static readonly RNGCryptoServiceProvider cryptoRandom = new RNGCryptoServiceProvider();
        private static readonly Random random = new Random();

        internal static ulong GetULong(bool crypto)
        {
            if (crypto) cryptoRandom.GetBytes(buffer);
            else random.NextBytes(buffer);

            return buffer.ULongFromBytes(0);
        }
    }
}
