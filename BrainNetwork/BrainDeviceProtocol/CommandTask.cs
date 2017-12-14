using System;
using System.Threading;
using System.Threading.Tasks;
using BrainCommon;
using BrainNetwork.RxSocket.Common;

namespace BrainNetwork.BrainDeviceProtocol
{
    public enum CommandError
    {
        Success,
        Failed,
        Timeout,
        SendingCommand,
        CommandNotFound,
    }
    
    /// <summary>
    /// use compiler option DisableDevTimeout to disable checking device response time out
    /// </summary>
    public partial class DevCommandSender
    {
        private int _taskLockTag = CASHelper.LockFree;
        private LastCommand _lastCommand;
        private TaskCompletionSource<CommandError> _currentTaskCtl;
        private CancellationTokenSource _cts;

        public async Task<CommandError> ExecCmd(DevCommandEnum cmd, params object[] args)
        {
            if (!_cmdhandler.TryGetValue(cmd, out var handler))
            {
                AppLogger.Error("Device Command Not Found:");
                return CommandError.CommandNotFound;
            }

            var task = new LastCommand {Cmd = cmd, FuncId = handler.FuncId};
            if (handler.DontCheckResponse) task = null;
            
            var isSending = false;
            
            //spin lock
            while (Interlocked.CompareExchange(ref _taskLockTag, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree)
            {
            }
            if (_lastCommand != null)
                isSending = true;
            else
                _lastCommand = task;
            //free lock
            Interlocked.CompareExchange(ref _taskLockTag, CASHelper.LockFree, CASHelper.LockUsed);
            
            if (isSending) return CommandError.SendingCommand;
            
            
            var buf = new RecycleBuffer(handler.CntSize, _bufMgr);
            var buffer = buf.Buffer;
            var cmdState = handler.FillCnt(buffer, args);
            buffer[0] = handler.FuncId;

            if (handler.DontCheckResponse)
            {
                _clientFrameSender.OnNext(DisposableValue.Create(new ArraySegment<byte>(buffer), buf));
                handler.HandlerSuccess(cmdState);
                return CommandError.Success;
            }

            _currentTaskCtl = new TaskCompletionSource<CommandError>();
            _clientFrameSender.OnNext(DisposableValue.Create(new ArraySegment<byte>(buffer), buf));

#if !DisableDevTimeout
            ScheduleTimeout();
#endif

            var result = await _currentTaskCtl.Task;
            if (result == CommandError.Success)
                handler.HandlerSuccess(cmdState);
            return result;
        }

        private async void ScheduleTimeout()
        {
            try
            {
                await Task.Delay(100,_cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            TaskCompletionSource<CommandError> ctl = null;
            var isSending = false;
            //spin lock
            while (Interlocked.CompareExchange(ref _taskLockTag, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree)
            {
            }
            if (_lastCommand != null)
            {
                isSending = true;
                _lastCommand = null;
                ctl = _currentTaskCtl;
                _currentTaskCtl = null;
            }
            //free lock
            Interlocked.CompareExchange(ref _taskLockTag, CASHelper.LockFree, CASHelper.LockUsed);
            
            if (!isSending) return;
            
            ctl?.SetResult(CommandError.Timeout);
        }

        public void CommitResponse(ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                AppLogger.Error("invalid data");
                return;
            }
            if (data.Count <= 0)
            {
                AppLogger.Error("invalid data");
                return;
            }

            var funcId = data.Array[data.Offset];
            LastCommand task = null;
            TaskCompletionSource<CommandError> ctl = null;
            
            //spin lock
            while (Interlocked.CompareExchange(ref _taskLockTag, CASHelper.LockUsed, CASHelper.LockFree) != CASHelper.LockFree)
            {
            }
            if (_lastCommand != null && _lastCommand.FuncId == funcId)
            {
                task = _lastCommand;
                _lastCommand = null;
                ctl = _currentTaskCtl;
                _currentTaskCtl = null;
            }
            //free lock
            Interlocked.CompareExchange(ref _taskLockTag, CASHelper.LockFree, CASHelper.LockUsed);

            if (task == null) return;
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            
            if (!_cmdhandler.TryGetValue(task.Cmd, out var handler))
            {
                ctl.SetResult(CommandError.Failed);
                return;
            }
            if (handler.ReponseHasErrorFlag)
            {
                var success = data.Array[data.Offset + 1] == 0;
                ctl.SetResult(success ? CommandError.Success : CommandError.Failed);
                return;
            }
            ctl.SetResult(CommandError.Success);
        }
        
        private class LastCommand
        {
            public DevCommandEnum Cmd;
            public byte FuncId;
        }
    }
}