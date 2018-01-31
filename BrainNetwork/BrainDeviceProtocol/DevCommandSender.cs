using System;
using System.Collections.Generic;
using System.Threading;
using BrainCommon;

namespace BrainNetwork.BrainDeviceProtocol
{
    public interface ICommandContent
    {
        DevCommandEnum CmdName { get; }
        /// <summary>
        /// 按照协议发送的字节大小，包括一个字节的FunctionID，不包括贞头贞尾
        /// </summary>
        int CntSize { get; }
        byte FuncId { get; }
        /// <summary>
        /// 对应命令放大器是否有回应数据包
        /// </summary>
        bool DontCheckResponse { get; }
        /// <summary>
        /// 如果有回应数据包，是否包含错误标记
        /// </summary>
        bool ReponseHasErrorFlag { get; }
        object FillCnt(byte[] buffer, object[] args);
        /// <summary>
        /// 仅当命令发送成功，并且回应没有出错或者超时，才会调用，
        /// 没有回应的命令，发送成功就马上执行这个回调
        /// </summary>
        /// <param name="cmdCnt"></param>
        void HandlerSuccessAsync(object cmdCnt);
    }

    public sealed partial class DevCommandSender
    {
        private readonly IObserver<DisposableValue<ArraySegment<byte>>> _clientFrameSender;
        private readonly SyncBufManager _bufMgr;
        private readonly Dictionary<DevCommandEnum, ICommandContent> _cmdhandler;
        private volatile bool _enableTimeout;
        private volatile uint _sendTimeout=100;

        public DevCommandSender(IObserver<DisposableValue<ArraySegment<byte>>> clientFrameSender,
            SyncBufManager bufMgr, IObservable<(bool,uint)> enableTimeoutConfig)
        {
            _cmdhandler = new Dictionary<DevCommandEnum, ICommandContent>();
            foreach (var cmdcnt in ReflectionHelper.GetAllInterfaceImpl<ICommandContent>())
            {
                AddCommand(cmdcnt);
            }
            
            _clientFrameSender = clientFrameSender;
            _bufMgr = bufMgr;
            _cts = new CancellationTokenSource();
            enableTimeoutConfig?.Subscribe(pair => (_enableTimeout, _sendTimeout) = pair);
        }

        public void AddCommand(ICommandContent cnt)
        {
            _cmdhandler.Add(cnt.CmdName, cnt);
        }

        /*public void ExecCmd(DevCommandEnum cmd, params object[] args)
        {
            if (!_cmdhandler.TryGetValue(cmd, out var handler))
            {
                AppLogger.Log("Device Command Not Found:");
                return;
            }
            var buf = new RecycleBuffer(handler.CntSize, _bufMgr);
            var buffer = buf.Buffer;
            handler.FillCnt(buffer, args);
            buffer[0] = handler.FuncId;
            _clientFrameSender.OnNext(DisposableValue.Create(new ArraySegment<byte>(buffer), buf));
        }*/
    }
}