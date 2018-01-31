using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using WaveletStudio;
using WaveletStudio.Functions;
using WaveletStudio.Wavelet;

namespace BrainCommon
{
    public class WaveletReconstructionConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ConvolutionModeEnum ConvolutionMode;
        [JsonConverter(typeof(StringEnumConverter))]
        public SignalExtension.ExtensionMode ExtensionMode;
        public string MotherWaveletName;//MotherWavelet Name
        public int Level;
        //
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