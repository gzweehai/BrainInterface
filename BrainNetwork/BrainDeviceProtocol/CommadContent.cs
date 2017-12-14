using System.Threading.Tasks;

namespace BrainNetwork.BrainDeviceProtocol
{
    public enum DevCommandEnum
    {
        Start,
        Stop,
        SetSampleRate,
        SetTrap,
        SetFilter,
        QueryParam,
    }

    public sealed partial class DevCommandSender
    {
        #region Start Command

        public class FillStartCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.Start;
            public int CntSize => 2;
            public byte FuncId => 1;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => false;

            public void FillCnt(byte[] buffer, object[] args)
            {
                buffer[1] = 1;
            }
        }

        public async Task<CommandError> Start()
        {
            return await ExecCmd(DevCommandEnum.Start);
        }

        #endregion

        #region Stop Command

        public class FillStopCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.Stop;
            public int CntSize => 2;
            public byte FuncId => 1;
            public bool DontCheckResponse => true;
            public bool ReponseHasErrorFlag => false;

            public void FillCnt(byte[] buffer, object[] args)
            {
                buffer[1] = 0;
            }
        }

        public async Task<CommandError> Stop()
        {
            return await ExecCmd(DevCommandEnum.Stop);
        }

        #endregion

        #region Set Sampling Rate Command

        public class FillSetSampleRateCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.SetSampleRate;
            public int CntSize => 2;
            public byte FuncId => 11;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public void FillCnt(byte[] buffer, object[] args)
            {
                var rate = (SampleRateEnum) args[0];
                byte rateB = 0;
                switch (rate)
                {
                    case SampleRateEnum.SPS_250:
                        rateB = 1;
                        break;
                    case SampleRateEnum.SPS_500:
                        rateB = 2;
                        break;
                    case SampleRateEnum.SPS_1k:
                        rateB = 3;
                        break;
                    case SampleRateEnum.SPS_2k:
                        rateB = 4;
                        break;
                }
                buffer[1] = rateB;
            }
        }

        public enum SampleRateEnum
        {
            SPS_250,
            SPS_500,
            SPS_1k,
            SPS_2k,
        }

        public async Task<CommandError> SetSampleRate(SampleRateEnum sampleRate)
        {
            return await ExecCmd(DevCommandEnum.SetSampleRate, sampleRate);
        }

        #endregion

        #region Set Trap Command

        public class FillSetTrapCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.SetTrap;
            public int CntSize => 2;
            public byte FuncId => 12;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public void FillCnt(byte[] buffer, object[] args)
            {
                var trapOpt = (TrapSettingEnum) args[0];
                byte opt = 0;
                switch (trapOpt)
                {
                    case TrapSettingEnum.NoTrap:
                        opt = 0;
                        break;
                    case TrapSettingEnum.Trap_50:
                        opt = 10;
                        break;
                    case TrapSettingEnum.Trap_60:
                        opt = 11;
                        break;
                }
                buffer[1] = opt;
            }
        }

        public enum TrapSettingEnum
        {
            NoTrap,
            Trap_50,
            Trap_60,
        }

        public async Task<CommandError> SetTrap(TrapSettingEnum trapOption)
        {
            return await ExecCmd(DevCommandEnum.SetTrap, trapOption);
        }

        #endregion
        
        #region Set Filter Command

        public class FillSetFilterCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.SetFilter;
            public int CntSize => 2;
            public byte FuncId => 13;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public void FillCnt(byte[] buffer, object[] args)
            {
                var useFilter = (bool) args[0];
                buffer[1] = useFilter ? (byte)1 : (byte)0;
            }
        }

        public async Task<CommandError> SetFilter(bool useFilter)
        {
            return await ExecCmd(DevCommandEnum.SetFilter, useFilter);
        }

        #endregion
        
        #region Query Parameters Command

        public class FillQueryCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.QueryParam;
            public int CntSize => 1;
            public byte FuncId => 21;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public void FillCnt(byte[] buffer, object[] args)
            {
            }
        }

        public async Task<CommandError> QueryParam()
        {
            return await ExecCmd(DevCommandEnum.QueryParam);
        }

        #endregion
    }
}