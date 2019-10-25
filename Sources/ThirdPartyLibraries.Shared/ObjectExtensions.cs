namespace ThirdPartyLibraries.Shared
{
    public static class ObjectExtensions
    {
        public static int CombineHashCodes(int h1, int h2)
        {
            return (int)((uint)(h1 << 5) | (uint)h1 >> 27) + h1 ^ h2;
        }

        public static int CombineHashCodes(int h1, int h2, int h3)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), h3);
        }
    }
}
