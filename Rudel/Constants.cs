namespace Rudel
{
    internal static class Constants
    {
        internal const ushort MAX_OUTGOING_MESSAGE_DELAY = 1000;
        internal const ushort SOCKET_BLOCK_MILISECONDS = 10;
        internal const ushort CONNECTION_TIMEOUT_MILISECONDS = 10000;
        internal const byte CONNECTION_SEGMENT_RETRIES = 10;
        internal const int CONNECTION_SEGMENT_RETRY_TIMEOUT = 1000;
        internal const byte ACK_MASK_BITS = 64;
        internal const ushort DOS_AMP_PROTECTION_SIZE = 1024;
        internal const ushort SEQUENCE_MESSAGE_BUFFER_SIZE = 1024;
        internal const string HAIL_MESSAGE = "HAIL";
        internal const string BYE_MESSAGE = "BYE";
        internal const bool USE_CRYPTO_RANDOM = true;
        internal static readonly byte[] PROTOCOL_ESTABLISHMENT_PREFIX = {
            68, 166, 237,
            24, 71, 188,
            66, 117, 115,
            111, 171, 137,
            77, 105, 100,
            76, 101, 118,
            101, 108, 208,
            181, 129, 157,
            219, 158, 52,
            214, 175, 146,
            198, 36
        };
    }
}
