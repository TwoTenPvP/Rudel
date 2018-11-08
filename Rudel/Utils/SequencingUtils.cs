namespace Rudel.Utils
{
    internal class SequencingUtils
    {
        internal static long Distance(ulong from, ulong to, byte bytes)
        {
            int _shift = (sizeof(ulong) - bytes) * sizeof(byte);

            to <<= _shift;
            from <<= _shift;

            return ((long)(from - to)) >> _shift;
        }
    }
}