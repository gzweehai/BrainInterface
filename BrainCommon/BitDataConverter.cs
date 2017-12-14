namespace BrainCommon
{
    public class BitDataConverter
    {
        /// <summary>
        /// convert 24 bit data from ADS1299, to sample value 
        /// </summary>
        /// <param name="b0">high byte</param>
        /// <param name="b1">mid byte</param>
        /// <param name="b2">low byte</param>
        /// <param name="vRef"></param>
        /// <param name="gain"></param>
        /// <returns></returns>
        public static double ConvertFrom(byte b0,byte b1,byte b2,float vRef=4.5f, int gain=72)
        {
            const int maxVal = 0x7fFFff;
            var signFlag = b0 & 0x80;
            if (signFlag == 0)
            {
                //positive
                var num = From24bit(b0, b1, b2);
                return (num * vRef) / (maxVal * gain);
            }
            else
            {
                //negative
                var num = From24bit((byte) (b0 & 0x7f), b1, b2);
                num--;
                
                byte c2 = (byte)(num & 0xff);
                num = num >> 8;
                byte c1 = (byte)(num & 0xff);
                num = num >> 8;
                byte c0 = (byte)((num & 0xff) | 0x80);

                num = From24bit(Xor(c0), Xor(c1),Xor(c2));
                
                
                return -(num * vRef) / (maxVal * gain);
            }
        }

        private static byte Xor(byte c0)
        {
            return (byte)(c0 ^ 0xff);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b0">high byte</param>
        /// <param name="b1">mid byte</param>
        /// <param name="b2">low byte</param>
        /// <returns></returns>
        private static int From24bit(byte b0, byte b1, byte b2)
        {
            var num = b0 & 0x7f;
            num = (num << 8) + b1;
            num = (num << 8) + b2;
            return num;
        }
    }
}