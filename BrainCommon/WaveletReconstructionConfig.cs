using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WaveletStudio;
using WaveletStudio.Functions;
using WaveletStudio.Wavelet;

namespace BrainCommon
{
    /// <summary>
    /// 使用小波变换进行分析过滤的配置，
    /// 优先于其他滤波算法执行，
    /// 通过对WindowSize大小的数据流进行Level层级的分解，
    /// 然后对Approximate部分求均值进行基线漂移过滤
    /// </summary>
    public class WaveletReconstructionConfig
    {
        public bool DisableWavelet;
        [JsonConverter(typeof(StringEnumConverter))]
        public ConvolutionModeEnum ConvolutionMode;
        [JsonConverter(typeof(StringEnumConverter))]
        public SignalExtension.ExtensionMode ExtensionMode;
        public string MotherWaveletName;//MotherWavelet Name
        public int Level;
        
        public int WindowSize;
        public int AvgLevel;

        public WaveletReconstruction Create(int sampleRate)
        {
            var motherWavelet = CommonMotherWavelets.GetWaveletFromName(MotherWaveletName);
            if (motherWavelet == null) return null;
            return new WaveletReconstruction(WindowSize,ConvolutionMode,
                ExtensionMode,motherWavelet,Level,sampleRate,AvgLevel);
        }
    }
}