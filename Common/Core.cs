using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public delegate int InitailizeProgress();
    public delegate void UpdateProgress(int progress);
    public class Redirection
    {
        public bool Handled = false;
        public Message Message = null;
        public Redirection(Message msg)
        {
            Message = msg;
        }
    }
    public class Device
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string LastAddr { get; set; }
    }
    public enum CoreType
    {
        Sender, Receiver
    }
    public class Core
    {
        protected const int ReceiverPort = 37384;
        protected const int SenderPort = 38384;
        protected const int TransferPort = 37484;
        public readonly int PortUsed;
        public readonly int PortUnused;
        private readonly UdpClient client;
        protected const int Timeout = 5000;
        protected readonly IPAddress MulticastAddr = IPAddress.Parse("234.2.3.4");
        protected readonly IPAddress LocalAddr = IPAddress.Parse(Utils.GetLocalIPAddress());

        static string deviceListFile = "devices";
        public void OnAndroidDevice()
        {
            deviceListFile = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "devices");
        }
        public Core(CoreType t)
        {
            switch (t)
            {
                case CoreType.Sender:
                    PortUsed = SenderPort;
                    PortUnused = ReceiverPort;
                    break;
                case CoreType.Receiver:
                    PortUsed = ReceiverPort;
                    PortUnused = SenderPort;
                    break;
                default:
                    throw new Exception();
            }
            client = new UdpClient(PortUsed);
            client.JoinMulticastGroup(MulticastAddr);
        }
        protected void CallWithTimeout(Action action, int miliseconds)
        {
            Task wrapper = new Task(() => action());
            using CancellationTokenSource cancellation = new CancellationTokenSource(miliseconds);
            wrapper.Start();
            wrapper.Wait(cancellation.Token);
        }
        protected CancellationTokenSource CallAtBackground(Action action)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Task wrapper = new Task(() => action(), cancellation.Token);
            wrapper.Start();
            return cancellation;
        }
        protected void UdpSend(IPEndPoint remoteEP, Message msg)
        {
            byte[] bs = msg.ToBytes();
            //using (UdpClient client = new UdpClient(PortUsed))
            {
                client.Send(bs, bs.Length, remoteEP);
                //DEBUG
                Console.WriteLine("Message:\n{0}\nSent to {1}:{2}", msg, remoteEP.Address.ToString(), remoteEP.Port);
            }
        }
        protected void UdpReceive(ref IPEndPoint remoteEP, Predicate<Message> condition, Action<Message> callback)
        {
            //using (UdpClient client = new UdpClient(PortUsed))
            {
                while (true)
                {
                    Console.WriteLine("Start listening at {0}:{1}", remoteEP.Address, remoteEP.Port);

                    byte[] receive = client.Receive(ref remoteEP);

                    Console.WriteLine("Received: {0}", Utils.ShowBytes(receive));
                    if (Message.TryParse(receive, out Message rMessage) && condition(rMessage))
                    {
                        //DEBUG
                        Console.WriteLine("Message:\n{0}\nReceive from {1}:{2}", rMessage, remoteEP.Address.ToString(), remoteEP.Port);
                        callback(rMessage);
                        return;
                    }
                }
            }
        }
        protected void UdpMulticastSend(Message message)
        {
            IPEndPoint multicastEP = new IPEndPoint(MulticastAddr, PortUnused);
            UdpSend(multicastEP, message);
        }
        protected void UdpMulticastReceive(ref IPEndPoint remoteEP, Predicate<Message> condition, Action<Message> callback)
        {
            //using (UdpClient client = new UdpClient(PortUsed))
            {
                while (true)
                {
                    //client.JoinMulticastGroup(MulticastAddr);
                    Console.WriteLine("Start listening at {0}:{1}", remoteEP.Address, remoteEP.Port);
                    byte[] receive = client.Receive(ref remoteEP);
                    Console.WriteLine("Received: {0}", Utils.ShowBytes(receive));

                    if (Message.TryParse(receive, out Message rMessage) && condition(rMessage))
                    {
                        //DEBUG
                        Console.WriteLine("Message:\n{0}\nReceive from {1}:{2}", rMessage, remoteEP.Address.ToString(), remoteEP.Port);
                        callback(rMessage);
                        return;
                    }
                }
            }
        }
        protected void TcpSetupStream(IPEndPoint remoteEP, Action<NetworkStream> streamAction)
        {
            using TcpClient client = new TcpClient();
            client.Connect(remoteEP);
            if (client.Connected)
            {
                var ns = client.GetStream();
                streamAction(ns);
                ns.Close(); client.Close();
            }
        }
        protected void TcpAcceptStream(Action<NetworkStream> streamAction)
        {
            TcpListener listener = new TcpListener(LocalAddr, TransferPort);
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();
            var ns = client.GetStream();
            streamAction(ns);
            ns.Close();
            listener.Stop();
        }
        protected void SaveDevice(string key, string lastAddr)
        {
            string name = null;
            while (true)
            {
                Console.Write("Device name: ");
                name = Console.ReadLine();
                if (!ReadDevices().Exists(d => d.Name == name))
                {
                    break;
                }
                Console.Error.WriteLine("Duplicate name, retry.");
            }
            using StreamWriter sw = new StreamWriter(deviceListFile);
            sw.Write($"{(name == "" ? "unamed device" : name)}" + ',' + lastAddr + ',' + key + '\n');
        }
        protected void SaveDevice(Device device)
        {
            var devices = ReadDevices();
            if (devices.Exists(d => d.Name == device.Name && d.Key == device.Key))
            {
                devices[devices.FindIndex(d => d.Name == device.Name)] = device;
                SaveDevice(devices);
            }
            else
            {
                using StreamWriter sw = new StreamWriter(deviceListFile);
                sw.WriteLine(device.Name + ',' + device.LastAddr + ',' + device.Key + '\n');
            }
        }
        protected void SaveDevice(List<Device> devices)
        {
            using FileStream fs = new FileStream(deviceListFile, FileMode.Truncate);
            foreach (var d in devices)
            {
                byte[] bs = Encoding.Default.GetBytes(d.Name + ',' + d.LastAddr + ',' + d.Key + '\n');
                fs.Write(bs, 0, bs.Length);
            }
        }
        public List<Device> ReadDevices()
        {
            List<Device> devices = new List<Device>();
            if (!File.Exists(deviceListFile))
            {
                File.Create(deviceListFile).Close();
            }
            using (StreamReader sr = new StreamReader(deviceListFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    char[] s = new char[] { ',' };
                    string[] i = line.Split(s, 3);
                    devices.Add(new Device { Name = i[0], LastAddr = i[1], Key = i[2] });
                }
            }
            return devices;
        }
        protected Device FindKey(string key2)
        {
            var devices = ReadDevices();
            if (devices.Exists(s => s.Key.Contains(key2)))
            {
                return devices.Find(s => s.Key.Contains(key2));
            }
            else return null;
        }
    }

}
