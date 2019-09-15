using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Security.Cryptography;

namespace Transfer
{
    class Sender : Core
    {
        int PackSize { get => 1024; }
        public void AddDeviceFromScanning()
        {
            IPAddress localAddr = IPAddress.Parse(Utils.GetLocalIPAddress());
            IPAddress remoteAddr = null;
            Message MsgSent = new Message { IP = localAddr, Pin = Utils.GeneratePin() };
            string status = null;
            bool confirmed = false;

            try
            {
                UdpMulticastSend(MsgSent);
                CallWithTimeout(()=> {
                    status = "Get Response";
                    UdpMulticastReceive((msg) => msg.Type == MsgType.Info && msg.Pin == MsgSent.Pin,
                        (msg) => remoteAddr = msg.IP);
                }, Timeout);

                MsgSent = new Message { Key = Utils.GenerateKey() };
                UdpSend(new IPEndPoint(remoteAddr, InPort), MsgSent);
                
                CallWithTimeout(()=> {
                    status = "Get Confirm";
                    UdpReceive(new IPEndPoint(remoteAddr, OutPort),
                        (msg) => msg.Type == MsgType.Key && msg.Key == MsgSent.Key,
                        (msg) => confirmed = true);
                }, Timeout);

                if (confirmed)
                {
                    SaveDevice(MsgSent.Key, remoteAddr.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                remoteAddr = null;
                Console.Error.WriteLine(status + " Timeout");
            }
            
        }
        public void ListDevices()
        {
            List<Device> devices = ReadDevices();
            string fmt = "{0,-10}    {1,-15}    ";
            Console.WriteLine(fmt, "Device Name", "Last Connnected Addr");
            foreach (var d in devices)
            {
                Console.WriteLine(fmt,d.Name,d.LastAddr);
            }
        }
        public void SendFile(string name,string filename)
        {
            List<Device> devices = ReadDevices();
            Device device = devices.Find((d) => d.Name == name);
            devices = null;
            Message meta = Message.CreateMeta(filename, PackSize, out byte[] data);
            string key1, key2, status = null;
            key1 = Utils.GenerateSemiKey(device.Key, out key2);
            Message confirm = new Message { SemiKey = key1, IP = LocalAddr };
            IPAddress remoteAddr = null;
            long continueId = 0;
            try
            {
                UdpMulticastSend(confirm);
                CallWithTimeout(() =>
                {
                    status = "Confirm IP";
                    UdpMulticastReceive((msg) => msg.Type == MsgType.Confirm && msg.SemiKey == key2,
                        (msg) => { device.LastAddr = msg.IP.ToString(); remoteAddr = msg.IP; });
                }, Timeout);
                UdpSend(new IPEndPoint(remoteAddr, InPort), meta);
                CallWithTimeout(() =>
                {
                    status = "Confirm Process";
                    UdpReceive(new IPEndPoint(remoteAddr, OutPort),
                        (msg) => msg.Type == MsgType.Continue,
                        (msg) => continueId = msg.PackID);
                }, Timeout);
                //Set up Tcp connection
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(remoteAddr, InPort);
                    if (client.Connected)
                    {
                        NetworkStream s = client.GetStream();
                        byte[] bs = null;
                        Message dataMsg = new Message();
                        for (int i =(int)continueId-1; i < meta.PackCount - 1; i++)
                        {
                            dataMsg.PackID = i + 1;
                            dataMsg.Data = data.AsSpan().Slice(i * PackSize, PackSize).ToArray();
                            bs = dataMsg.ToBytes();
                            s.Write(bs, 0, bs.Length);
                        }
                        dataMsg.PackID++;
                        dataMsg.Data = data.AsSpan().Slice((int)(dataMsg.PackID - 2) * PackSize).ToArray();
                        bs = dataMsg.ToBytes();
                        s.Write(bs, 0, bs.Length);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine(status+" Timeout");
            }
            catch(SocketException e)
            {
                Console.Error.WriteLine("Transfer Error");
                throw e;
            }
        }
    }
}
