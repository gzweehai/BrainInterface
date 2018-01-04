using System.Threading.Tasks;
using BrainCommon;

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

    public enum DevCommandFuncId
    {
        StartStop=1,
        SetSampleRate=0x11,
        SetTrap=0x12,
        SetFilter=0x13,
        QueryParam=0x21,
    }

    public static partial class BrainDeviceManager
    {
        #region Start Command

        public class FillStartCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.Start;
            public int CntSize => 2;
            public byte FuncId => 1;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => false;

            public object FillCnt(byte[] buffer, object[] args)
            {
                buffer[1] = 1;
                return null;
            }

            public void HandlerSuccess(object cmdCnt)
            {
                CommitStartStop(true);
            }
        }

        #endregion
        #region Stop Command

        public class FillStopCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.Stop;
            public int CntSize => 2;
            public byte FuncId => (byte)DevCommandFuncId.StartStop;
            public bool DontCheckResponse => true;
            public bool ReponseHasErrorFlag => false;

            public object FillCnt(byte[] buffer, object[] args)
            {
                buffer[1] = 0;
                return null;
            }

            public async void HandlerSuccess(object cmdCnt)
            {//set stop tag
                await Task.Delay(50);
                CommitStartStop(false);
            }
        }

        #endregion
        #region Set Sampling Rate Command

        public class FillSetSampleRateCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.SetSampleRate;
            public int CntSize => 2;
            public byte FuncId => (byte)DevCommandFuncId.SetSampleRate;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public object FillCnt(byte[] buffer, object[] args)
            {
                var rate = (SampleRateEnum) args[0];
                buffer[1] = (byte)rate;
                return rate;
            }

            public void HandlerSuccess(object cmdCnt)
            {
                var rateB = (SampleRateEnum)cmdCnt;
                CommitSampleRate(rateB);
            }
        }

        #endregion
        #region Set Trap Command

        public class FillSetTrapCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.SetTrap;
            public int CntSize => 2;
            public byte FuncId => (byte)DevCommandFuncId.SetTrap;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public object FillCnt(byte[] buffer, object[] args)
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
                return trapOpt;
            }

            public void HandlerSuccess(object cmdCnt)
            {
                var trapOpt = (TrapSettingEnum)cmdCnt;
                CommitTrapOpt(trapOpt);
            }
        }

        #endregion
        
        #region Set Filter Command

        public class FillSetFilterCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.SetFilter;
            public int CntSize => 2;
            public byte FuncId => (byte)DevCommandFuncId.SetFilter;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => true;

            public object FillCnt(byte[] buffer, object[] args)
            {
                var useFilter = (bool) args[0];
                buffer[1] = useFilter ? (byte)1 : (byte)0;
                return useFilter;
            }

            public void HandlerSuccess(object cmdCnt)
            {
                var useFilter = (bool) cmdCnt;
                CommitEnableFiler(useFilter);
            }
        }

        #endregion
    }

    public sealed partial class DevCommandSender
    {
        public async Task<CommandError> Start()
        {
            return await ExecCmd(DevCommandEnum.Start);
        }

        public async Task<CommandError> Stop()
        {
            return await ExecCmd(DevCommandEnum.Stop);
        }

        public async Task<CommandError> SetSampleRate(SampleRateEnum sampleRate)
        {
            return await ExecCmd(DevCommandEnum.SetSampleRate, sampleRate);
        }

        public async Task<CommandError> SetTrap(TrapSettingEnum trapOption)
        {
            return await ExecCmd(DevCommandEnum.SetTrap, trapOption);
        }

        public async Task<CommandError> SetFilter(bool useFilter)
        {
            return await ExecCmd(DevCommandEnum.SetFilter, useFilter);
        }

        
        #region Query Parameters Command

        public class FillQueryCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.QueryParam;
            public int CntSize => 1;
            public byte FuncId => (byte)DevCommandFuncId.QueryParam;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => false;

            public object FillCnt(byte[] buffer, object[] args)
            {
                return null;
            }

            public void HandlerSuccess(object cmdCnt)
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