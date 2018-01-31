using System;
using System.Collections.Generic;
using MathNet.Filtering;
using MathNet.Filtering.FIR;
using MathNet.Filtering.FIR.FilterRangeOp;
using MathNet.Filtering.Median;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BrainCommon
{
    public class FilterTypeList
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public List<FilterType> Filters;
        public const string SampleRateOptionName = "SampleRate";
        public const string FIRhalfOrderOptionName = "FIRhalfOrder";
        
        /// <summary>
        /// For FIR filters, you must fill SampleRate (double) and FIRhalfOrder (int) into parameters  
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public IOnlineFilter CreateFilter(Dictionary<string, string> parameters)
        {
            var tmp = new IOnlineFilter[Filters.Count];
            for (var i = 0; i < Filters.Count; i++)
            {
                tmp[i] = Filters[i].CreateFilter(parameters);
            }

            return new SeqCombinedOnlineFilter(tmp);
        }
    }
    
    public abstract class FilterType
    {
        public abstract IOnlineFilter CreateFilter(Dictionary<string, string> parameters);
    }

    public class MedianFilter:FilterType
    {
        public int HalfMedianWindowSize;
        public override IOnlineFilter CreateFilter(Dictionary<string, string> parameters)
        {
            return new OnlineFastMedianFilter(HalfMedianWindowSize);
        }
    }

    public class BandPassStopFilter : FilterType
    {
        public int HalfOrder = 5;

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public List<BandFilter> BandFilterList;

        public override IOnlineFilter CreateFilter(Dictionary<string, string> parameters)
        {
            double sampleRate = parameters["SampleRate"].ToDouble();
            var halfOrder=parameters["FIRhalfOrder"].ToInt();
            IFirFilterRangeCollections tmp = null; 
            for (var i = 0; i < BandFilterList.Count; i++)
            {
                var filterParam = BandFilterList[i];
                var pri=filterParam.CreatePrimitiveBandFilter();
                tmp = tmp == null ? pri : tmp.Add(pri);
            }

            var cof = tmp.GetFirCoefficients(sampleRate, halfOrder);
            return new OnlineFirFilter(cof);
        }
    }

    public abstract class BandFilter
    {
        public abstract PrimitiveFilterRange CreatePrimitiveBandFilter();
    }

    public class LowPassFilter : BandFilter
    {
        public int LowPassRate;

        public override PrimitiveFilterRange CreatePrimitiveBandFilter()
        {
            return new LowPassRange(LowPassRate);
        }
    }

    public class HighPassFilter : BandFilter
    {
        public int HighPassRate;

        public override PrimitiveFilterRange CreatePrimitiveBandFilter()
        {
            return new HighPassRange(HighPassRate);
        }
    }

    public class BandPassFilter : BandFilter
    {
        public int LowCutoffRate;
        public int HighCutoffRate;

        public override PrimitiveFilterRange CreatePrimitiveBandFilter()
        {
            return new BandPassRange(LowCutoffRate,HighCutoffRate);
        }
    }

    public class BandStopFilter : BandFilter
    {
        public int LowPassRate;
        public int HighPassRate;

        public override PrimitiveFilterRange CreatePrimitiveBandFilter()
        {
            return new BandStopRange(LowPassRate,HighPassRate);
        }
    }
}