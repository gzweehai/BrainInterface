using System;
using System.Collections.Generic;
using BrainCommon;

namespace BrainNetwork.BrainDeviceProtocol
{
    /// <summary>
    /// 对应某个FunctionID的回应处理器
    /// </summary>
    public interface IReceivedDataProcessor
    {
        byte FuncId { get; }
        void Process(ArraySegment<byte> data);
    }

    /// <summary>
    /// 接收数据的处理器，对于采样数据，因为放大器是连续不断的发送包的，需要特殊处理，
    /// 其余数据包对应某个命令的回应，执行注册的处理器
    /// </summary>
    public sealed class ReceivedDataProcessor
    {
        public static readonly ReceivedDataProcessor Instance = new ReceivedDataProcessor();

        private readonly Dictionary<byte, IReceivedDataProcessor> _processorMap;

        private BrainNetwork.BrainDeviceProtocol.DevCommandSender _sender;
        public BrainNetwork.BrainDeviceProtocol.DevCommandSender Sender
        {
            set => _sender = value;
        }

        private ReceivedDataProcessor()
        {
            _processorMap = new Dictionary<byte, IReceivedDataProcessor>();
            foreach (var processor in ReflectionHelper.GetAllInterfaceImpl<IReceivedDataProcessor>())
            {
                _processorMap.Add(processor.FuncId,processor);
            }
        }

        public void AddProcessor(IReceivedDataProcessor processor)
        {
            _processorMap.Add(processor.FuncId,processor);
        }

        public bool Process(ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                AppLogger.Error("invalid data");
                return false;
            }
            if (data.Count <= 0)
            {
                AppLogger.Error("invalid data");
                return false;
            }

            var funcId = data.Array[data.Offset];
            if (!_processorMap.TryGetValue(funcId, out var processor))
            {
                AppLogger.Error($"function id not register:{funcId}");
                return false;
            }
            try
            {
                processor.Process(data);
#if DEBUG
                AppLogger.Debug($"recieved data:{data.Show()}");
#endif
                return true;
            }
            finally
            {
                _sender?.CommitResponse(data);
            }
        }
    }
}