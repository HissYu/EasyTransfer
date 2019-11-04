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
        private Status status = new Status { Device = new Device { }, FileName = "", Current = 0, PackCount = 100 };

        private TaskCompletionSource<bool> deviceSelected;
        public MainWindow()
        {
            InitializeComponent();
            
            
            sender = new Sender();

            StatusBar.DataContext = status;

            DeviceRemoval();
            BindEvent();
            StartBackground();
        }

        private async void DeviceRemoval()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    List<Device> t = AroundDeviceList.ItemsSource as List<Device>;
                    if (t == null) continue;
                    t = t.AsParallel().Where(device => device.Time >= DateTime.Now.AddSeconds(-4)).ToList();
                    AroundDeviceList.Dispatcher.Invoke(() =>
                    {
                        AroundDeviceList.ItemsSource = t;
                    });
                }
            }).ConfigureAwait(false);
        }

        private void BindEvent()
        {
            Core.OnDeviceFound += (List<Device> devices) => {
                if (devices.Count>0)
                {

                    AroundDeviceList.Dispatcher.Invoke(() =>
                    {
                        AroundDeviceList.ItemsSource = devices.Concat((AroundDeviceList.ItemsSource as List<Device>)??new List<Device>());
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
            Core.OnTransferError += (state) =>
            {
                MessageBox.Show(state.Data);
            };
        }

        private void StartBackground()
        {
            
            sender.FindDeviceAroundAsync();
            //receiver.StartWorking();
        }

        private async void SendClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("No text exists in clipboard!");
            }
            
            string clipb = Clipboard.GetText();

            MessageBox.Show("Choose a device.");
            deviceSelected = new TaskCompletionSource<bool>();
            await deviceSelected.Task;
            string addr = (AroundDeviceList.SelectedItem as Device).Addr;
            deviceSelected = null;
            this.sender.SendTextAsync(addr, clipb);
        }

        private async void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            

            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                status.Update(new Status { FileName = System.IO.Path.GetFileName(filename) });

                MessageBox.Show("Choose a device.");
                deviceSelected = new TaskCompletionSource<bool>();
                await deviceSelected.Task;
                string addr = (AroundDeviceList.SelectedItem as Device).Addr;
                deviceSelected = null;
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
            status.Update(new Status { Device = ((sender as ListBox).SelectedItem as Device) });
            deviceSelected?.SetResult(true);
        }
    }
}
