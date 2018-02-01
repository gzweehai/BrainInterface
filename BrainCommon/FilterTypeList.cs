﻿using System.Collections.Generic;
using MathNet.Filtering;
using MathNet.Filtering.FIR;
using MathNet.Filtering.FIR.FilterRangeOp;
using MathNet.Filtering.Median;
using Newtonsoft.Json;

namespace BrainCommon
{
    /// <summary>
    /// 滤波设置，按照顺序创建对应的滤波器，
    /// 支持中值滤波，低通滤波，高通滤波，带通滤波，带阻滤波，以及混合多个滤波器
    /// </summary>
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
            var tmp = new List<IOnlineFilter>(Filters.Count);
            for (var i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].Disable) continue;
                tmp.Add(Filters[i].CreateFilter(parameters));
            }

            if (tmp.Count <= 0)
                return EmptyFilter.Instance;
            if (tmp.Count == 1)
                return tmp[0];

            return new SeqCombinedOnlineFilter(tmp.ToArray());
        }
    }
    
    public abstract class FilterType
    {
        public bool Disable;
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
                if (filterParam.Disable) continue;
                var pri=filterParam.CreatePrimitiveBandFilter();
                tmp = tmp == null ? pri : tmp.Add(pri);
            }

            if (tmp == null) return null;
            var cof = tmp.GetFirCoefficients(sampleRate, halfOrder);
            return new OnlineFirFilter(cof);
        }
    }

    public abstract class BandFilter
    {
        public bool Disable;
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