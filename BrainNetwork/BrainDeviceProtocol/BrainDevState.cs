using System.Collections.Generic;

namespace BrainNetwork.BrainDeviceProtocol
{
    public struct BrainDevState
    {
        public byte DevCode;
        public byte ChannelCount;
        public byte Gain;//放大倍数
        public SampleRateEnum SampleRate;
        public TrapSettingEnum TrapOption;
        public bool EnalbeFilter;
        public const int StoreSize = 6;
        
        public bool IsStart;
        public byte FaultStateCode;
        public byte LastSelectedSingleImpedanceChannel;
        public int LastSingleImpedanceCode;
        public List<int> LastMultiImpedanceCodes;
        
        
        public const int SampleCount_250 = 20 * 250 / 1000;
        public const int SampleCount_500 = 20 * 500 / 1000;
        public const int SampleCount_1k = 20 * 1000 / 1000;
        public const int SampleCount_2k = 20 * 2000 / 1000;

        public const float SmapleDeltaTime_250 = 1000f / 250;
        public const float SmapleDeltaTime_500 = 1000f / 500;
        public const float SmapleDeltaTime_1k = 1000f / 1000;
        public const float SmapleDeltaTime_2k = 1000f / 2000;

        public static int SampleCountPer20ms(SampleRateEnum sampleRate)
        {
            var count = 1;
            switch (sampleRate)
            {
                case SampleRateEnum.SPS_250: //every 1000ms sample 250 times
                    count = BrainDevState.SampleCount_250; //20ms -> sample counts
                    break;
                case SampleRateEnum.SPS_500:
                    count = BrainDevState.SampleCount_500; //20ms -> sample counts
                    break;
                case SampleRateEnum.SPS_1k:
                    count = BrainDevState.SampleCount_1k; //20ms -> sample counts
                    break;
                case SampleRateEnum.SPS_2k:
                    count = BrainDevState.SampleCount_2k; //20ms -> sample counts
                    break;
            }
            return count;
        }

        public static float PassTimeMs(SampleRateEnum sampleRate,int passTimeTick)
        {
            var count = 1f;
            switch (sampleRate)
            {
                case SampleRateEnum.SPS_250: //every 1000ms sample 250 times
                    count = passTimeTick*BrainDevState.SmapleDeltaTime_250;
                    break;
                case SampleRateEnum.SPS_500:
                    count = passTimeTick*BrainDevState.SmapleDeltaTime_500;
                    break;
                case SampleRateEnum.SPS_1k:
                    count = passTimeTick*BrainDevState.SmapleDeltaTime_1k;
                    break;
                case SampleRateEnum.SPS_2k:
                    count = passTimeTick*BrainDevState.SmapleDeltaTime_2k;
                    break;
            }
            return count;
        }
        
        public override string ToString()
        {
            return $"{nameof(DevCode)}: {DevCode}, {nameof(ChannelCount)}: {ChannelCount}, {nameof(Gain)}: {Gain}, {nameof(SampleRate)}: {SampleRate}, {nameof(TrapOption)}: {TrapOption}, {nameof(EnalbeFilter)}: {EnalbeFilter}, {nameof(IsStart)}: {IsStart}, {nameof(FaultStateCode)}: {FaultStateCode}, {nameof(LastSelectedSingleImpedanceChannel)}: {LastSelectedSingleImpedanceChannel}, {nameof(LastSingleImpedanceCode)}: {LastSingleImpedanceCode}, " +
                   $"{nameof(LastMultiImpedanceCodes)}: {LastMultiImpedanceCodes?.Count}";
        }

        public int GetChannelImpedance(int i)
        {
            if (LastMultiImpedanceCodes != null && i < LastMultiImpedanceCodes.Count)
            {
                return LastMultiImpedanceCodes[i];
            }
            return 0;
        }
    }

    public enum TrapSettingEnum
    {
        NoTrap,
        Trap_50,
        Trap_60,
    }

    public enum SampleRateEnum
    {
        SPS_2k =3,
        SPS_1k =4,
        SPS_500=5,
        SPS_250=6,
    }
}