using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transfer
{
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
        public string Name;
        public string Key;
        public string LastAddr;
    }
    public class Core
    {
        protected const int ReceiverPort = 37384;
        protected const int SenderPort = 38384;
        protected const int TransferPort = 37484;
        protected readonly int PortUsed;
        protected const int Timeout = 5000;
        protected readonly IPAddress MulticastAddr = IPAddress.Parse("234.2.3.4");
        protected readonly IPAddress LocalAddr = IPAddress.Parse(Utils.GetLocalIPAddress());

        protected void CallWithTimeout(Action action, int miliseconds)
        {
            Task wrapper = new Task(() => action());
            using (CancellationTokenSource cancellation = new CancellationTokenSource(miliseconds))
            {
                wrapper.Start();
                wrapper.Wait(cancellation.Token);
            }
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
            using (UdpClient client = new UdpClient(PortUsed))
            {
                client.Send(bs, bs.Length, remoteEP);
                //DEBUG
                Console.WriteLine("Message:\n{0}\nSent to {1}:{2}", msg, remoteEP.Address.ToString(), remoteEP.Port);
            }
        }
        protected void UdpReceive(ref IPEndPoint remoteEP, Predicate<Message> condition, Action<Message> callback)
        {
            while (true)
            {
                using (UdpClient client = new UdpClient(PortUsed))
                {
                    byte[] receive = client.Receive(ref remoteEP);
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
            IPEndPoint multicastEP = new IPEndPoint(MulticastAddr, PortUsed);
            UdpSend(multicastEP, message);
        }
        protected void UdpMulticastReceive(ref IPEndPoint remoteEP ,Predicate<Message> condition, Action<Message> callback)
        {
            while (true)
            {
                using (UdpClient client = new UdpClient(PortUsed))
                {
                    client.JoinMulticastGroup(MulticastAddr);
                    //IPEndPoint multicastEP = new IPEndPoint(MulticastAddr, SenderPort);
                    byte[] receive = client.Receive(ref remoteEP);
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
        protected void SaveDevice(string key, string lastAddr)
        {
            using (StreamWriter sw = new StreamWriter("devices"))
            {
                string name = null;
                while (true)
                {
                    Console.Write("Device name: ");
                    name = Console.ReadLine();
                    if (!ReadDevices().Exists(d=>d.Name==name))
                    {
                        break;
                    }
                    Console.Error.WriteLine("Duplicate name, retry.");
                }
                sw.Write($"{(name == "" ? "unamed device" : name)}" + ',' + lastAddr + ',' + key + '\n');
            }
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
                using (StreamWriter sw = new StreamWriter("devices"))
                {
                    sw.WriteLine(device.Name + ',' + device.LastAddr + ',' + device.Key + '\n');
                }
            }
        }
        protected void SaveDevice(List<Device> devices)
        {
            using (FileStream fs = new FileStream("devices",FileMode.Truncate))
            {
                foreach (var d in devices)
                {
                    fs.Write(Encoding.Default.GetBytes(d.Name + ',' + d.LastAddr + ',' + d.Key + '\n'));
                }
            }
        }
        public List<Device> ReadDevices()
        {
            List<Device> devices = new List<Device>();
            using (StreamReader sr = new StreamReader("devices"))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] i = line.Split(',', 3);
                    devices.Add(new Device { Name = i[0], LastAddr = i[1], Key = i[2] });
                }
            }
            return devices;
        }
    }
}
