using MathNet.Filtering;

namespace BrainCommon
{
    public sealed class EmptyFilter:IOnlineFilter
    {
        public static readonly IOnlineFilter Instance = new EmptyFilter();
        private EmptyFilter(){}

        public double ProcessSample(double sample) => sample;

        public double[] ProcessSamples(double[] samples) => samples;

        public void Reset()
        {
        }
    }
}