// *************************************************************************************
// SCICHART ?Copyright ABT Software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.scichart.com
// Support: info@abtsoftware.co.uk
//  
// BaseViewModel.cs is part of SciChart Examples, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Abt.Controls.SciChart.Example.MVVM
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {        
        public event PropertyChangedEventHandler PropertyChanged;

#if SILVERLIGHT
        private static Dispatcher _dispatcher;
        public BaseViewModel()
        {
            TryGetDispatcher();
        }

        private void TryGetDispatcher()
        {
            try
            {
                if (Application.Current.RootVisual != null && _dispatcher == null)
                    _dispatcher = Application.Current.RootVisual.Dispatcher;
            }
            catch
            {
            }
        }
#endif

        protected void OnPropertyChanged(string propertyName)
        {            
#if SILVERLIGHT
            if (_dispatcher == null) TryGetDispatcher();

            if (_dispatcher != null && !_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(() => OnPropertyChanged(propertyName));
                return;
            }
#endif            

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}