using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Microsoft.Win32;

namespace TransferWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Sender sender;
        //private Receiver receiver = new Receiver();
        private CancellationTokenSource current;
        private Status status = new Status { DeviceName = "test", FileName = "file Test", Current = 0, PackCount = 100 };

        public MainWindow()
        {
            InitializeComponent();
            
            current = new CancellationTokenSource();
            sender = new Sender(ref current);

            StatusBar.DataContext = status;
            
            
            BindEvent();
            StartBackground();
        }

        private void BindEvent()
        {
            Core.OnDeviceFound += (List<Device> devices) => {
                if (devices.Count>0)
                {
                    AroundDeviceList.Dispatcher.Invoke(() =>
                    {
                        AroundDeviceList.ItemsSource = devices;
                    });
                }
            };
            Core.OnReceivedRequest += (meta, isText) =>
            {
                if (isText) return true;
                MessageBoxResult result = MessageBox.Show($"Filename: {meta.Filename}\nSize: {Utils.FormatFileSize(meta.PackCount * meta.PackSize)}", "New file to send to your device, accept?", MessageBoxButton.OKCancel);
                return result == MessageBoxResult.OK;
            };
            Core.OnPackTransfered += (process) =>
            {
                status.Update(new Status { Current = process });
            };
        }

        private void StartBackground()
        {
            sender.FindDeviceAroundAsync();
            //receiver.StartWorking();
        }

        private void SendClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (AroundDeviceList.SelectedItem == null)
            {
                MessageBox.Show("You need to select a device to send to!");
                return;
            }
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("No text exists in clipboard!");
            }
            string addr = (AroundDeviceList.SelectedItem as Device).Addr;
            string clipb = Clipboard.GetText();

            this.sender.SendTextAsync(addr, clipb);
            //StopButton.IsEnabled = true;
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (AroundDeviceList.SelectedItem == null)
            {
                MessageBox.Show("You need to select a device to send to!");
                return;
            }
            string addr = (AroundDeviceList.SelectedItem as Device).Addr;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All Files (*.*)|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                status.Update(new Status { FileName = System.IO.Path.GetFileName(filename) });
                
                this.sender.SendFileAsync(addr, filename);
                StopButton.IsEnabled = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            //current.Cancel();
            this.sender.FindDeviceAroundAsync();
            status.Update(new Status { Current = 0,FileName="" });
            StopButton.IsEnabled = false;
        }

        private void AroundDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            status.Update(new Status { DeviceName = ((sender as ListBox).SelectedItem as Device)?.Name });
        }
    }
}
