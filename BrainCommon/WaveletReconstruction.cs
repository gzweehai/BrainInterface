using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using WaveletStudio;
using WaveletStudio.Functions;
using WaveletStudio.Wavelet;

namespace BrainCommon
{
    /// <summary>
    /// 使用小波变换进行分析过滤，
    /// 应该优先于其他滤波算法执行，
    /// 通过对WindowSize大小的数据流进行Level层级的分解，
    /// 然后对Approximate部分求均值进行基线漂移过滤
    /// </summary>
    public class WaveletReconstruction
    {
        private int _windowSize;
        private ConvolutionModeEnum _convolutionMode;
        private SignalExtension.ExtensionMode _extensionMode;
        private MotherWavelet _motherWavelet;
        private int _level;
        private int _avgLevel;

        private int _sampleRate;
        private Subject<(double, float)> _reconstructionStream;
        private List<(double,float)> _buffer;
        
        public int WindowSize
        {
            get => _windowSize;
            set => _windowSize = value;
        }

        public ConvolutionModeEnum ConvolutionMode
        {
            get => _convolutionMode;
            set => _convolutionMode = value;
        }

        public SignalExtension.ExtensionMode ExtensionMode
        {
            get => _extensionMode;
            set => _extensionMode = value;
        }

        public MotherWavelet MotherWavelet
        {
            get => _motherWavelet;
            set => _motherWavelet = value;
        }

        public int Level
        {
            get => _level;
            set => _level = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="windowSize"></param>
        /// <param name="convolutionMode"></param>
        /// <param name="extensionMode"></param>
        /// <param name="motherWavelet"></param>
        /// <param name="level"></param>
        /// <param name="sampleRate"></param>
        /// <param name="avgLevel"></param>
        public WaveletReconstruction(int windowSize, ConvolutionModeEnum convolutionMode, 
            SignalExtension.ExtensionMode extensionMode, MotherWavelet motherWavelet,
            int level, int sampleRate,int avgLevel)
        {
            _windowSize = windowSize;
            _convolutionMode = convolutionMode;
            _extensionMode = extensionMode;
            _motherWavelet = motherWavelet;
            _level = level;
            _sampleRate = sampleRate;
            _avgLevel = avgLevel;
            _reconstructionStream = new Subject<(double, float)>();
            _buffer = new List<(double, float)>(windowSize);
        }
        
        public Subject<(double, float)> ReconstructionStream => _reconstructionStream;

        public void BufferData((double, float) sinalData)
        {
            _buffer.Add(sinalData);
            if (_buffer.Count >= _windowSize)
            {
                var buf = _buffer;
                _buffer = new List<(double, float)>(_windowSize);
                var motherWavelet = _motherWavelet;
                var level = _level;
                var convolutionModeEnum = _convolutionMode;
                
                var voltageLine = new double[buf.Count];
                for (var i = 0; i < buf.Count; i++)
                {
                    voltageLine[i] = buf[i].Item1;
                }

                var lel=DWT.ExecuteDWT(new Signal(voltageLine,_sampleRate), motherWavelet, level, _extensionMode, convolutionModeEnum);
                AverageLevel(lel,_avgLevel);
                //Reconstruction
                voltageLine = DWT.ExecuteIDWT(lel, motherWavelet, level, convolutionModeEnum);
                
                for (var i = 0; i < buf.Count; i++)
                {
                    _reconstructionStream.OnNext((voltageLine[i], buf[i].Item2));
                }
            }
        }

        private void AverageLevel(List<DecompositionLevel> deLvl, int avgLevel)
        {
            if (avgLevel < 1)
                avgLevel = 0;
            else
                avgLevel--;
            if (avgLevel >= deLvl.Count)
                avgLevel = deLvl.Count - 1;

            var approximation = deLvl[avgLevel].Approximation;
            var avg = approximation.Average();
            for (var i = 0; i < approximation.Length; i++)
            {
                approximation[i] = avg;
            }
        }

        public void Reset()
        {
            _buffer.Clear();
        }
    }
}