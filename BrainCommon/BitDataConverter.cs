using System;

namespace BrainCommon
{
    public class BitDataConverter
    {
        /// <summary>
        /// convert 24 bit data from ADS1299, to sample value 
        /// TODO use unsafe to improve
        /// Mac int four byte order: b2 b1 b0 (fill zero byte)
        /// TODO windows byte order:
        /// </summary>
        /// <param name="b0">high byte</param>
        /// <param name="b1">mid byte</param>
        /// <param name="b2">low byte</param>
        /// <param name="vRef"></param>
        /// <param name="gain"></param>
        /// <returns></returns>
        public static double ConvertFrom(byte b0,byte b1,byte b2,float vRef=4.5f, int gain=72)
        {
            var num = (b0 << 16) + (b1 << 8) + b2;
            if ((b0 & 0x80) != 0)
            {
                num = num - flipVal;
            }
            return (num * vRef) / (maxVal * gain);
        }
        
        const int maxVal = 0x7fFFff;
        const int flipVal = (maxVal + 1) * 2;

        public static int ConvertFrom(byte[] arr,int ind)
        {
            var num = (arr[ind] << 16) + (arr[ind + 1] << 8) + arr[ind + 2];
            if ((arr[ind] & 0x80) != 0)
            {
                num -= flipVal;
            }
            return num;
        }

        public static int[] ConvertFromNoInline(ArraySegment<byte> data)
        {
            var arr = data.Array;
            if (arr == null) return null;
            var ind = data.Offset;
            var count = data.Count;
            var result = new int[count/3];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ConvertFrom(arr,ind);
                ind += 3;
            }
            return result;
        }

        public static int[] ConvertFrom(ArraySegment<byte> data)
        {
            var arr = data.Array;
            if (arr == null) return null;
            var ind = data.Offset;
            var count = data.Count;
            var result = new int[count/3];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (arr[ind] << 16) + (arr[ind + 1] << 8) + arr[ind + 2];
                if ((arr[ind] & 0x80) != 0)
                {
                    result[i] -= flipVal;
                }
                ind += 3;
            }
            return result;
        }

        public static int[] FastConvertFrom(ArraySegment<byte> data)
        {
            if (data.Array == null) return null;
            var startInd = data.Offset;
            var count = data.Count;
            var result = new int[count/3];
            unsafe
            {
                fixed (byte* arr = data.Array)
                {
                    fixed (int* r = result)
                    {
                        int* rt = r;
                        for (int i = startInd; i < count; i = i + 3)
                        {
                            byte* tmp = (byte*) rt;
                            *tmp = arr[i+2];
                            tmp++;
                            *tmp = arr[i+1];
                            tmp++;
                            *tmp = arr[i];
                            tmp++;
                            *tmp = 0;
                            if ((arr[i] & 0x80) != 0)
                            {
                                *rt -= flipVal;
                            }
                            rt++;
                        }
                    }
                }
                
            }
            return result;
        }

        private static byte[] PrepareTestData()
        {
            //const int count = 32*40;//max count expected
            const int count = 10;//simple test
            const int startNum = maxVal - count / 2;
            var tArr = new byte[count * 3];
            for (var i = 0; i < count; i++)
            {
                var num = startNum + i;
                unsafe
                {
                    byte* p = (byte*) &num;
                    tArr[i * 3 + 2] = *p;
                    p++;
                    tArr[i * 3 + 1] = *p;
                    p++;
                    tArr[i * 3] = *p;
                }
            }
            return tArr;
        }

        public static void TestConvertPerformance()
        {
            var tArr = PrepareTestData();
            Console.WriteLine(tArr.Show());
            
            var startTime = DateTime.Now;
            var result = ConvertFrom(new ArraySegment<byte>(tArr));
            var endTime = DateTime.Now;
            var timeSpanNormal = endTime - startTime;
            Console.WriteLine($"time used:{timeSpanNormal}");
            Console.WriteLine(result.Show());
            
            startTime = DateTime.Now;
            var result2 = FastConvertFrom(new ArraySegment<byte>(tArr));
            endTime = DateTime.Now;
            var timeSpanFast = endTime - startTime;
            Console.WriteLine($"time used:{timeSpanFast}");
            Console.WriteLine(result2.Show());
            
            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] != result2[i])
                {
                    Console.WriteLine($"bug at index:{i}, {result[i]} != {result2[i]}");
                }
            }
            Console.WriteLine($"FastConvertFrom is realy faster than ConvertFrom? {timeSpanFast<timeSpanNormal},faster order :{timeSpanNormal.Ticks/timeSpanFast.Ticks},{timeSpanNormal},{timeSpanFast}");
        }
        
        public static void TestConvert()
        {
            var tArr = PrepareTestData();
            Console.WriteLine(tArr.Show());
            var result = ConvertFrom(new ArraySegment<byte>(tArr));
            Console.WriteLine(result.Show());
        }

        public static void TestUnsafeConvert()
        {
            var tArr = PrepareTestData();
            Console.WriteLine(tArr.Show());
            var result = FastConvertFrom(new ArraySegment<byte>(tArr));
            Console.WriteLine(result.Show());
        }
        
        public static void TestByteOrder()
        {
            int number = 0x7feeff;
            
            unsafe 
            {
                // Convert to byte:
                byte* p = (byte*)&number;
            
                System.Console.Write("The 4 bytes of the integer:");
            
                // Display the 4 bytes of the int variable:
                for (int i = 0 ; i < sizeof(int) ; ++i)
                {
                    System.Console.Write(" {0:X2}", *p);
                    // Increment the pointer:
                    p++;
                }
                System.Console.WriteLine();
                System.Console.WriteLine("The value of the integer: {0}", number);
            
                // Keep the console window open in debug mode.
                System.Console.WriteLine("Press any key to exit.");
                System.Console.ReadKey();
            }
        }
    }
}