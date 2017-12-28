using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Abt.Controls.SciChart.Example.Common
{
    public class HyperlinkButton: Button, INotifyPropertyChanged
    {
        private string _navigateUri;

        public string NavigateUri
        {
            get { return _navigateUri; }
            set
            {
                _navigateUri = value;

                OnPropertyChanged("NavigateUri");
            }
        }

        public string TargetName
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
