namespace BrainCommon
{
    public static class LengthBitConverter
    {
        public static int FromByte(byte[] data, int startInd, int count)
        {
            //TODO allow count <= 4
            return data[startInd];
        }
    }
}