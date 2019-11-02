using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TransferWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

    internal class Status : INotifyPropertyChanged
    {
        public string DeviceName { get => devicename; set { devicename = value;OnPropertyChanged(); } }
        string devicename;
        public string FileName { get=>filename; set { filename = value;OnPropertyChanged(); } }
        string filename;
        public double? Current { get=>current; set { current = value;OnPropertyChanged(); } }
        double? current;
        public double? PackCount { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                //Console.WriteLine(propertyName);
                var e = new PropertyChangedEventArgs(propertyName);
                this.PropertyChanged(this, e);
            }
        }
        public void Update(Status status)
        {
            DeviceName = status.DeviceName ?? this.DeviceName;
            FileName = status.FileName ?? this.FileName;
            Current = status.Current ?? this.Current;
            PackCount = status.PackCount ?? this.PackCount;
        }
    }
}
