// *************************************************************************************
// SCICHART © Copyright ABT Software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.scichart.com
// Support: info@abtsoftware.co.uk
//  
// TimedMethod.cs is part of SciChart Examples, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the SciChart license, see http://www.scichart.com/scichart-eula/
//  
// SciChart Examples source code is provided free of charge on an "As-Is" basis to support
// and provide examples of how to use the SciChart component. You bear the risk of using it. 
// The authors give no express warranties, guarantees or conditions. You may have additional 
// consumer rights under your local laws which this license cannot change. To the extent 
// permitted under your local laws, the contributors exclude the implied warranties of 
// merchantability, fitness for a particular purpose and non-infringement. 
// *************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace Abt.Controls.SciChart.Example.Common
{
    public class TimedMethod : IDisposable
    {
        private readonly Action _action;
        private int _milliseconds = 100;
        private DispatcherTimer _timer;

        private TimedMethod(Action action)
        {
            _action = action;
        }

        public static TimedMethod Invoke(Action action)
        {
            return new TimedMethod(action);
        }

        public TimedMethod After(int milliseconds)
        {
            _milliseconds = milliseconds;
            return this;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public TimedMethod Go()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(_milliseconds);
            _timer.Tick += (s, e) =>
                               {
                                   _action();
                                   _timer.Stop();
                               }; 
            _timer.Start();
            return this;
        }
    }
}
