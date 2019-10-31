using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common;

namespace TransferWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Sender sender = new Sender();
        //private Receiver receiver = new Receiver();
        public MainWindow()
        {
            InitializeComponent();
            Core.OnDeviceFound += delegate (List<Device> devices) {
                AroundDeviceList.Dispatcher.Invoke(() =>
                {
                    AroundDeviceList.ItemsSource = devices;
                });
            };

            //List<Device> devices = new List<Device>();
            //devices.Add(new Device { Name = "1", Addr = "10.9.9.9" });
            //devices.Add(new Device { Name = "2", Addr = "10.9.9.10" });
            //AroundDeviceList.ItemsSource = devices;
            
            StartBackground();
        }

        private void StartBackground()
        {
            sender.FindDeviceAround();
            //receiver.StartWorking();
        }
    }
}
