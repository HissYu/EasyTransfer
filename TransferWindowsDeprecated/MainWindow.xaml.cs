using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Sender sender = new Sender();
        private readonly Receiver receiver = new Receiver();
        public MainWindow()
        {
            InitializeComponent();
            deviceList.DataContext = sender.ReadDevices();
        }

        private void DeviceList_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (deviceList.SelectedIndex == -1)
            {
                deviceList.ContextMenu.IsEnabled = false;
                return;
            }
            
        }

    }
}
