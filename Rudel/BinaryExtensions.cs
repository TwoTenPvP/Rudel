using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rudel
{
    internal static class BinaryExtensions
    {
        internal static ulong ULongFromBytes(this byte[] b, int offset)
        {
            return (
                ((uint)b[offset]) |
                ((ulong)b[offset + 1] << 8) |
                ((ulong)b[offset + 2] << 16) |
                ((ulong)b[offset + 3] << 24) |
                ((ulong)b[offset + 4] << 32) |
                ((ulong)b[offset + 5] << 40) |
                ((ulong)b[offset + 6] << 48) |
                ((ulong)b[offset + 7] << 56));
        }

        internal static ushort UShortFromBytes(this byte[] b, int offset)
        {
            return (ushort)(b[offset] | (b[offset + 1] << 8));
        }
    }
}
