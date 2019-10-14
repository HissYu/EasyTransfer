using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common
{
    public class Sender : Core
    {
        int PackSize { get => 1024; }
        public Sender() : base(CoreType.Sender) { }
        public Device AddDeviceFromScanning()
        {
            IPEndPoint remoteEP = new IPEndPoint(MulticastAddr, ReceiverPort);
            Message MsgSent = new Message { Pin = Utils.GeneratePin() };
            string status = null;
            bool confirmed = false;

            try
            {
                UdpMulticastSend(MsgSent);
                CallWithTimeout(() =>
                {
                    status = "Get Response";
                    UdpMulticastReceive(ref remoteEP,
                        (msg) => msg.Type == MsgType.Info && msg.Pin == MsgSent.Pin,
                        (msg) => { });
                }, Timeout);

                MsgSent = new Message { Key = Utils.GenerateKey() };
                UdpSend(remoteEP, MsgSent);

                CallWithTimeout(() =>
                {
                    status = "Get Confirm";
                    UdpReceive(ref remoteEP,
                        (msg) => msg.Type == MsgType.Key && msg.Key == MsgSent.Key,
                        (msg) => confirmed = true);
                }, Timeout);

                if (confirmed)
                {
                    //SaveDevice(MsgSent.Key, remoteEP.Address.ToString());
                    return new Device { Key = MsgSent.Key,LastAddr = remoteEP.Address.ToString()};
                }
            }
            catch (OperationCanceledException)
            {
                //Console.Error.WriteLine(status + " Timeout");
                UpdateState?.Invoke(new State(ActionCode.Connect, StateCode.Error, status + " Timeout"));
                return null;
            }
            return null;
        }
        public void ListDevices()
        {
            List<Device> devices = ReadDevices();
            string fmt = "{0,-10}    {1,-20}";
            Console.WriteLine(fmt, "Device Name", "Last Connnected Addr");
            foreach (var d in devices)
            {
                Console.WriteLine(fmt, d.Name, d.LastAddr);
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
                    (msg) => { });
            }, Timeout);
            confirm.SemiKey = key2;
            UdpSend(remoteEP, confirm);

            device.LastAddr = remoteEP.Address.ToString();
            SaveDevice(device);
            return remoteEP;
        }
        public void SendFile(string name, string filename)
        {

            Message meta = Message.CreateMeta(filename, PackSize);
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
                    using FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                    byte[] bs = null;
                    Message dataMsg = new Message { PackID = continueId, Data = new byte[PackSize] };
                    fs.Seek((continueId - 1) * PackSize, SeekOrigin.Begin);
                    while (fs.Length - fs.Position > PackSize)
                    {
                        fs.Read(dataMsg.Data, 0, PackSize);
                        bs = dataMsg.ToBytes();
                        ns.Write(bs, 0, bs.Length);
                        ns.Flush();
                        dataMsg.PackID++;
                    }
                    fs.Read(dataMsg.Data, 0, (int)(fs.Length - fs.Position));
                    bs = dataMsg.ToBytes();
                    ns.Write(bs, 0, bs.Length);
                    ns.Flush();
                });
            }
            catch (OperationCanceledException)
            {
                //Console.Error.WriteLine(status + " Timeout");
                UpdateState?.Invoke(new State(ActionCode.FileSend, StateCode.Error, status + " Timeout"));
            }
            catch (SocketException e)
            {
                //Console.Error.WriteLine("Transfer Error");
                UpdateState?.Invoke(new State(ActionCode.FileSend, StateCode.Error, e.Message));
                throw e;
            }
        }
        public void SendText(string name, string text)
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
                //Console.Error.WriteLine("Confirm Addr Timeout");
                UpdateState?.Invoke(new State(ActionCode.TextSend, StateCode.Error, "Confirm Addr Timeout"));

            }
            catch (SocketException e)
            {
                //Console.Error.WriteLine("Transfer Error");
                UpdateState?.Invoke(new State(ActionCode.TextSend, StateCode.Error, e.Message));
                throw e;
            }
        }
    }
}
