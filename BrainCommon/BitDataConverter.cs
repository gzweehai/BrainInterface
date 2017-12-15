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
            const int flipVal = (maxVal + 1) * 2;
            var num = From24bit(b0, b1, b2);
            var signFlag = b0 & 0x80;
            if (signFlag == 0)
            {
                //positive
                return (num * vRef) / (maxVal * gain);
            }
            else
            {
                num = flipVal - num;
                return -(num * vRef) / (maxVal * gain);
            }
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
            int num = b0;
            num = (num << 8) + b1;
            num = (num << 8) + b2;
            return num;
        }
    }
}