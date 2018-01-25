using System;

namespace RemoteGm
{
    public interface IFirFilterRangeCollections
    {
        PrimitiveFilterRange[] PrimitiveRanges { get; }
        double[] FirCoefficients { get; }
        IFirFilterRangeCollections Add(PrimitiveFilterRange range);
    }

    public abstract class PrimitiveFilterRange : IFirFilterRangeCollections
    {
        private readonly PrimitiveFilterRange[] _primitiveRanges;

        protected PrimitiveFilterRange()
        {
            _primitiveRanges = new[] {this};
        }

        public PrimitiveFilterRange[] PrimitiveRanges => _primitiveRanges;
        public abstract double[] FirCoefficients { get; }

        public IFirFilterRangeCollections Add(PrimitiveFilterRange range)
        {
            if (range == null) throw new ArgumentNullException(nameof(range));
            switch (range)
            {
                case LowPassRange t1:
                    return Add(t1);
                case HighPassRange t2:
                    return Add(t2);
                case BandWithRange t3:
                    return Add(t3);
                case BandStopRange t4:
                    return Add(t4);
            }
            throw new NotImplementedException($"can not add this range type:{range.GetType()}");
        }

        protected abstract IFirFilterRangeCollections Add(LowPassRange range);
        protected abstract IFirFilterRangeCollections Add(HighPassRange range);
        protected abstract IFirFilterRangeCollections Add(BandWithRange range);
        protected abstract IFirFilterRangeCollections Add(BandStopRange range);
    }
    
    public class LowPassRange:PrimitiveFilterRange
    {
        private readonly int _lowPassRate;

        public LowPassRange(int lowPassRate)
        {
            _lowPassRate = lowPassRate;
        }

        public override double[] FirCoefficients { get; }
        protected override IFirFilterRangeCollections Add(LowPassRange range)
        {
            return range._lowPassRate > _lowPassRate ? range : this;
        }

        protected override IFirFilterRangeCollections Add(HighPassRange range)
        {
            if (range.HighPassRate <= _lowPassRate) return AllRange.Instance;
            return new CombinedRange(this, range);
        }

        protected override IFirFilterRangeCollections Add(BandWithRange range)
        {
            if (_lowPassRate < range.LowCutoffRate) new CombinedRange(this, range);
            if (_lowPassRate < range.HighCutoffRate) new LowPassRange(range.HighCutoffRate);
            return this;
        }

        protected override IFirFilterRangeCollections Add(BandStopRange range)
        {
            if (range.LowPassRate <= _lowPassRate)
                throw new ArgumentException($"Pass and Stop range overlap: from {range.LowPassRate} to {_lowPassRate}");
            return new CombinedRange(this,range);
        }
    }

    public class HighPassRange:PrimitiveFilterRange
    {
        private readonly int _highPassRate;

        public HighPassRange(int highPassRate)
        {
            _highPassRate = highPassRate;
        }

        public override double[] FirCoefficients { get; }

        public int HighPassRate => _highPassRate;
    }
    
    public class BandWithRange:PrimitiveFilterRange
    {
        private int _lowCutoffRate;
        private int _highCutoffRate;
        public int LowCutoffRate => _lowCutoffRate;
        public int HighCutoffRate => _highCutoffRate;

        public BandWithRange(int lowCutoffRate, int highCutoffRate)
        {
            _lowCutoffRate = lowCutoffRate;
            _highCutoffRate = highCutoffRate;
        }

        public override double[] FirCoefficients { get; }
    }
    
    public class BandStopRange:PrimitiveFilterRange
    {
        private int _lowPassRate;
        private int _highPassRate;
        public int HighPassRate => _highPassRate;
        public int LowPassRate => _lowPassRate;

        public BandStopRange(int lowPassRate, int highPassRate)
        {
            _lowPassRate = lowPassRate;
            _highPassRate = highPassRate;
        }

        public override double[] FirCoefficients { get; }
    }
    
    public class CombinedRange:IFirFilterRangeCollections
    {
        public CombinedRange(LowPassRange lowPassRange, HighPassRange range)
        {
            throw new NotImplementedException();
        }

        public CombinedRange(LowPassRange lowPassRange, BandWithRange range)
        {
            throw new NotImplementedException();
        }

        public CombinedRange(LowPassRange lowPassRange, BandStopRange range)
        {
            throw new NotImplementedException();
        }

        public PrimitiveFilterRange[] PrimitiveRanges { get; }
        public double[] FirCoefficients { get; }
        public IFirFilterRangeCollections Add(PrimitiveFilterRange range)
        {
            throw new NotImplementedException();
        }
    }

    public class AllRange : IFirFilterRangeCollections
    {
        public PrimitiveFilterRange[] PrimitiveRanges => null;
        public double[] FirCoefficients => null;
        private static readonly AllRange _instance = new AllRange();
        public static IFirFilterRangeCollections Instance => _instance;
        private AllRange(){}

        public IFirFilterRangeCollections Add(PrimitiveFilterRange range)
        {
            return range;
        }
    }
}