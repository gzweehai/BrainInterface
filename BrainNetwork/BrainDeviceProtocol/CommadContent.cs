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
        QueryFaultState,
        TestSingleImpedance,
        TestMultiImpedance,
    }

    public enum DevCommandFuncId
    {
        StartStop=1,
        SetSampleRate=0x11,
        SetTrap=0x12,
        SetFilter=0x13,
        QueryParam=0x21,
        QueryFaultState=0x22,
        TestSingleImpedance=0x31,
        TestMultiImpedance=0x32,
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

            public void HandlerSuccessAsync(object cmdCnt)
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

            public async void HandlerSuccessAsync(object cmdCnt)
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

            public async void HandlerSuccessAsync(object cmdCnt)
            {
                var rateB = (SampleRateEnum)cmdCnt;
                CommitSampleRate(rateB);
                await Task.Delay(50);
                CommitStartStop(false);
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

            public void HandlerSuccessAsync(object cmdCnt)
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

            public void HandlerSuccessAsync(object cmdCnt)
            {
                var useFilter = (bool) cmdCnt;
                CommitEnableFiler(useFilter);
            }
        }

        #endregion

        #region TestSingleImpedance Command

        public class FillTestSingleImpedanceCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.TestSingleImpedance;
            public int CntSize => 2;
            public byte FuncId => (byte)DevCommandFuncId.TestSingleImpedance;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => false;

            public object FillCnt(byte[] buffer, object[] args)
            {
                var selectedChannel = (byte) args[0];
                buffer[1] = selectedChannel;
                return selectedChannel;
            }

            public void HandlerSuccessAsync(object cmdCnt)
            {
                var selectedChannel = (byte) cmdCnt;
                CommitSingleImpedanceChannel(selectedChannel);
            }
        }

        #endregion
        
        #region TestMultiImpedance Command

        public class FillTestMultiImpedanceCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.TestMultiImpedance;
            public int CntSize => 2;
            public byte FuncId => (byte)DevCommandFuncId.TestMultiImpedance;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => false;

            public object FillCnt(byte[] buffer, object[] args)
            {
                var channelCount = (byte) args[0];
                buffer[1] = channelCount;
                return args[0];
            }

            public void HandlerSuccessAsync(object cmdCnt)
            {
                //var channelCount = (byte) cmdCnt;
                //CommitMultiImpedanceCount(channelCount);
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

        public async Task<CommandError> TestSingleImpedance(byte selectedChannel)
        {
            return await ExecCmd(DevCommandEnum.TestSingleImpedance, selectedChannel);
        }
        
        public async Task<CommandError> TestMultiImpedance(byte channelCount)
        {
            return await ExecCmd(DevCommandEnum.TestMultiImpedance, channelCount);
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

            public void HandlerSuccessAsync(object cmdCnt)
            {
            }
        }
        
        #region QueryFaultState Command

        public class FillQueryFaultStateCommadContent : ICommandContent
        {
            public DevCommandEnum CmdName => DevCommandEnum.QueryFaultState;
            public int CntSize => 1;
            public byte FuncId => (byte)DevCommandFuncId.QueryFaultState;
            public bool DontCheckResponse => false;
            public bool ReponseHasErrorFlag => false;

            public object FillCnt(byte[] buffer, object[] args)
            {
                return null;
            }

            public void HandlerSuccessAsync(object cmdCnt)
            {
            }
        }

        #endregion
        

        public async Task<CommandError> QueryParam()
        {
            return await ExecCmd(DevCommandEnum.QueryParam);
        }
        
        public async Task<CommandError> QueryFaultState()
        {
            return await ExecCmd(DevCommandEnum.QueryFaultState);
        }

        #endregion
    }
}