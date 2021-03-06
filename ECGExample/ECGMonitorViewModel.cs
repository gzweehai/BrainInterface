﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Abt.Controls.SciChart.Example.MVVM;
using Abt.Controls.SciChart.Model.DataSeries;

namespace Abt.Controls.SciChart.Example.Examples.IWantTo.SeeFeaturedApplication.ECGMonitor
{
    public class ECGMonitorViewModel : BaseViewModel, IExampleAware
    {
        private Timer _timer;
        private IXyDataSeries<double, double> _series0;
        private double[] _sourceData;
        private int _currentIndex;
        private int _totalIndex;
        private DoubleRange _xVisibleRange;
        private DoubleRange _yVisibleRange;
        private bool _isBeat;
        private int _heartRate;
        private bool _lastBeat;
        private DateTime _lastBeatTime;

        private const double WindowSize = 5.0;
        private const int TimerInterval = 20;

        public ECGMonitorViewModel()
        {
            // Create a data series. We use FIFO series as we
            // want to discard old data after 5000pts
            // At the sample rate of ~500Hz and 5 seconds
            // visible range we'll need 2500 points in the FIFO. 
            // We set 5000 so no data gets discarded while still in view
            _series0 = new XyDataSeries<double, double>() { FifoCapacity = 15000 };

            // Simulate waveform
            _sourceData = LoadWaveformData("Waveform.txt");

            // Fix chart range in Y-Direction
            YVisibleRange = new DoubleRange(-0.5, 1.5);
        }

        /// <summary>
        /// SciChartSurface.DataSet binds to this
        /// </summary>
        public IXyDataSeries<double, double> EcgDataSeries
        {
            get { return _series0; }
            set
            {
                _series0 = value;
                OnPropertyChanged("EcgDataSeries");
            }
        }

        /// <summary>
        /// SciChartSurface.YAxis.VisibleRange binds to this
        /// </summary>
        public DoubleRange YVisibleRange
        {
            get { return _yVisibleRange; }
            set
            {
                _yVisibleRange = value;
                OnPropertyChanged("YVisibleRange");
            }
        }

        /// <summary>
        /// SciChartSurface.XAxis.VisibleRange binds to this
        /// </summary>
        public DoubleRange XVisibleRange
        {
            get { return _xVisibleRange; }
            set
            {
                if (!value.Equals(_xVisibleRange))
                {
                    _xVisibleRange = value;
                    OnPropertyChanged("XVisibleRange");
                }
            }
        }

        /// <summary>
        /// The heartbeat graphic binds to this, and changes its scale on heartbeat 
        /// </summary>
        public bool IsBeat
        {
            get { return _isBeat; }
            set
            {
                if (_isBeat != value)
                {
                    _isBeat = value;
                    OnPropertyChanged("IsBeat");
                }
            }
        }

        /// <summary>
        /// The heartrate textblock binds to this
        /// </summary>
        public int HeartRate
        {
            get { return _heartRate; }
            set
            {
                _heartRate = value;
                OnPropertyChanged("HeartRate");
            }
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            lock (this)
            {
                // As timer cannot tick quicker than ~20ms, we append 10 points
                // per tick to simulate a sampling frequency of 500Hz (e.g. 2ms per sample)
                for (int i = 0; i < 10; i++)
                    AppendPoint(400);

                // Assists heartbeat - it must show for 120ms before being deactivated
                if ((DateTime.Now - _lastBeatTime).TotalMilliseconds < 120) return;

                // Threshold the ECG voltage to determine if a heartbeat peak occurred
                IsBeat = _series0.YValues[_series0.Count - 3] > 0.5 ||
                         _series0.YValues[_series0.Count - 5] > 0.5 ||
                         _series0.YValues[_series0.Count - 8] > 0.5;

                // If so, compute the heart rate, update the last beat time
                if (IsBeat && !_lastBeat)
                {
                    HeartRate = (int)(60.0 / (DateTime.Now - _lastBeatTime).TotalSeconds);
                    _lastBeatTime = DateTime.Now;
                }
            }
        }

        private void AppendPoint(double sampleRate)
        {
            if (_currentIndex >= _sourceData.Length)
            {
                _currentIndex = 0;
            }

            // Get the next voltage and time, and append to the chart
            double voltage = _sourceData[_currentIndex];
            double time = _totalIndex / sampleRate;
            _series0.Append(time, voltage);

            // Calculate the next visible range
            XVisibleRange = ComputeXAxisRange(time);

            _lastBeat = IsBeat;
            _currentIndex++;
            _totalIndex++;
        }

        private static DoubleRange ComputeXAxisRange(double t)
        {
            if (t < WindowSize)
            {
                return new DoubleRange(0, WindowSize);
            }

            // Calculates a visible range. When the trace touches the right edge of the chart
            // (governed by WindowSize), shift the entire range 50% so that the trace is in the 
            // middle of the chart 
            double fractionSize = WindowSize * 0.5;
            double newMin = fractionSize * Math.Floor((t - fractionSize) / fractionSize);
            double newMax = newMin + WindowSize;

            return new DoubleRange(newMin, newMax);
        }

        private double[] LoadWaveformData(string filename)
        {
            var values = new List<double>();
            using (var stream = File.OpenRead(filename))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    string line = streamReader.ReadLine();
                    while (line != null)
                    {
                        values.Add(double.Parse(line, NumberFormatInfo.InvariantInfo));
                        line = streamReader.ReadLine();
                    }
                }
            }
            /*var asm = Assembly.GetExecutingAssembly();
            var resourceString = asm.GetManifestResourceNames().Single(x => x.Contains(filename));

            using (var stream = asm.GetManifestResourceStream(resourceString))
            using (var streamReader = new StreamReader(stream))
            {
                string line = streamReader.ReadLine();
                while (line != null)
                {
                    values.Add(double.Parse(line, NumberFormatInfo.InvariantInfo));
                    line = streamReader.ReadLine();
                }
            }*/

            return values.ToArray();
        }

        
        // These methods are just used to do tidy up when switching between examples
        public void OnExampleExit()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= TimerElapsed;
                _timer = null;
            }
        }

        public void OnExampleEnter()
        {
            _timer = new Timer(TimerInterval) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

    }
}