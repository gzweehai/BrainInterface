using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading;
using BrainCommon;

namespace BrainNetwork.BrainDeviceProtocol
{
    public interface ICommandContent
    {
        DevCommandEnum CmdName { get; }
        int CntSize { get; }
        byte FuncId { get; }
        bool DontCheckResponse { get; }
        bool ReponseHasErrorFlag { get; }
        object FillCnt(byte[] buffer, object[] args);
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