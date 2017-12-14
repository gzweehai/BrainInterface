namespace BrainNetwork.BrainDeviceProtocol
{
    public struct BrainDevState
    {
        public byte DevCode;
        public byte ChannelCount;
        public SampleRateEnum SampleRate;
        public TrapSettingEnum TrapOption;
        public bool EnalbeFilter;
        public bool IsStart;

        public override string ToString()
        {
            return $"{nameof(DevCode)}: {DevCode}, {nameof(ChannelCount)}: {ChannelCount}, {nameof(SampleRate)}: {SampleRate}, {nameof(TrapOption)}: {TrapOption}, {nameof(EnalbeFilter)}: {EnalbeFilter}, {nameof(IsStart)}: {IsStart}";
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
        SPS_250,
        SPS_500,
        SPS_1k,
        SPS_2k,
    }
}