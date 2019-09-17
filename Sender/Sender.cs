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
        //public new readonly int PortUsed = SenderPort;
        int PackSize { get => 1024; }
        public Sender() : base(CoreType.Sender) { }
        public void AddDeviceFromScanning()
        {
            IPEndPoint remoteEP = new IPEndPoint(MulticastAddr, ReceiverPort);
            Message MsgSent = new Message { Pin = Utils.GeneratePin() };
            string status = null;
            bool confirmed = false;

            try
            {
                UdpMulticastSend(MsgSent);
                CallWithTimeout(()=> {
                    status = "Get Response";
                    UdpMulticastReceive(ref remoteEP,
                        (msg) => msg.Type == MsgType.Info && msg.Pin == MsgSent.Pin,
                        (msg) => { });
                }, Timeout);

                MsgSent = new Message { Key = Utils.GenerateKey() };
                UdpSend(remoteEP, MsgSent);
                
                CallWithTimeout(()=> {
                    status = "Get Confirm";
                    UdpReceive(ref remoteEP,
                        (msg) => msg.Type == MsgType.Key && msg.Key == MsgSent.Key,
                        (msg) => confirmed = true);
                }, Timeout);

                if (confirmed)
                {
                    SaveDevice(MsgSent.Key, remoteEP.Address.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine(status + " Timeout");
            }
            
        }
        public void ListDevices()
        {
            List<Device> devices = ReadDevices();
            string fmt = "{0,-10}    {1,-20}    ";
            Console.WriteLine(fmt, "Device Name", "Last Connnected Addr");
            foreach (var d in devices)
            {
                Console.WriteLine(fmt,d.Name,d.LastAddr);
            }
        }
        private IPEndPoint ConfirmAddr(string name)
        {
            List<Device> devices = ReadDevices();
            Device device = devices.Find((d) => d.Name == name);
            devices = null;
            string key1, key2;
            key1 = Utils.GenerateSemiKey(device.Key, out key2);
            Message confirm = new Message { SemiKey = key1 };
            IPEndPoint remoteEP = new IPEndPoint(MulticastAddr, ReceiverPort);
            
            UdpMulticastSend(confirm);
            CallWithTimeout(() =>
            {
                UdpMulticastReceive(ref remoteEP,
                    (msg) => msg.Type == MsgType.Confirm && msg.SemiKey == key2,
                    (msg) => {  });
            }, Timeout);
            confirm.SemiKey = key2;
            UdpSend(remoteEP, confirm);

            device.LastAddr = remoteEP.Address.ToString();
            SaveDevice(device);
            return remoteEP;
        }
        public void SendFile(string name,string filename)
        {
            
            Message meta = Message.CreateMeta(filename, PackSize, out byte[] data);
            meta.PackSize = PackSize > data.Length ? data.Length : PackSize;
            string status = "";
            long continueId = 0;
            try
            {
                status = "Confirm Addr";
                IPEndPoint remoteEP = ConfirmAddr(name);
                UdpSend(remoteEP, meta);
                CallWithTimeout(() =>
                {
                    status = "Confirm Process";
                    UdpReceive(ref remoteEP,
                        (msg) => msg.Type == MsgType.Continue,
                        (msg) => continueId = msg.PackID);
                }, Timeout);
                remoteEP.Port = TransferPort;
                TcpSetupStream(remoteEP, ns =>
                {
                    byte[] bs = null;
                    Message dataMsg = new Message();
                    for (int i = (int)continueId - 1; i < meta.PackCount - 1; i++)
                    {
                        dataMsg.PackID = i + 1;
                        dataMsg.Data = data.AsSpan().Slice(i * PackSize, PackSize).ToArray();
                        bs = dataMsg.ToBytes();
                        ns.Write(bs, 0, bs.Length);
                        ns.Flush();
                    }
                    if (data.Length > (dataMsg.PackID) * PackSize)
                    {
                        dataMsg.PackID++;
                        dataMsg.Data = data.AsSpan().Slice((int)(dataMsg.PackID - 1) * PackSize).ToArray();
                        bs = dataMsg.ToBytes();
                        ns.Write(bs, 0, bs.Length);
                        ns.Flush();
                    }
                    //int sentbytes = ((int)continueId - 1)*PackSize;
                    //while (sentbytes<meta.Size)
                    //{
                    //    dataMsg.PackID++;
                    //    dataMsg.Data = data.AsSpan().Slice(sentbytes, PackSize).ToArray();
                    //    bs = dataMsg.ToBytes();
                    //    ns.Write(bs, 0, bs.Length);

                    //}
                });
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine(status + " Timeout");
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine("Transfer Error");
                throw e;
            }
        }
        public void SendText(string name,string text)
        {
            Message message = new Message { Text = text };
            try
            {
                IPEndPoint remoteEP = ConfirmAddr(name);
                UdpSend(remoteEP, Message.CreateTextMeta(text.Length));
                remoteEP.Port = TransferPort;
                TcpSetupStream(remoteEP, ns =>
                {
                    byte[] bs = message.ToBytes();
                    ns.Write(bs, 0, bs.Length);
                });
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Confirm Addr Timeout");
            }
            catch (SocketException e)
            {
                Console.Error.WriteLine("Transfer Error");
                throw e;
            }
        }
    }
}
