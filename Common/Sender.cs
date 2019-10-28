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
    public class Sender : Core
    {
        int PackSize { get => 1024; }
        public Sender() : base(CoreType.Sender) { }

        CancellationTokenSource aSocket = null;
        public void FindDeviceAround()
        {
            IPEndPoint remoteEP = new IPEndPoint(MulticastAddr, ReceiverPort);
            try
            {
                aSocket = CallAtBackground(() =>
                {
                    Message MsgSent = new Message();
                    while (true)
                    {
                        List<Device> devicesFound = new List<Device>();
                        MsgSent.Pin = Utils.GeneratePin();
                        UdpMulticastSend(MsgSent);
                        CallWithTimeout(() =>
                        {
                            UdpMulticastReceive(ref remoteEP,
                                (msg) => msg.Type == MsgType.Info && msg.Pin == MsgSent.Pin,
                                (msg) => {
                                    devicesFound.Add(new Device { Addr = remoteEP.Address.ToString(), Name = msg.DeviceName });
                                });
                        }, 2000);
                        OnDeviceFound?.Invoke(devicesFound);
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
        }
        public void StopBackgroudWork()
        {
            aSocket.Cancel();
            aSocket = null;
        }
        public void SendFile(string addr, string filename)
        {
            aSocket?.Cancel();
            Message meta = Message.CreateMeta(filename, PackSize);
            string status = "";
            long continueId = 0;
            try
            {
                status = "Wait Confirmation";
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(addr),ReceiverPort);
                UdpSend(remoteEP, meta);
                CallWithTimeout(() =>
                {
                    UdpReceive(ref remoteEP,
                        (msg) => msg.Type == MsgType.Continue,
                        (msg) => continueId = msg.PackID);
                }, 10000);
                remoteEP.Port = TransferPort;
                status = "Transferring";

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

                        OnPackTransfered?.Invoke(new State(ActionCode.FilePackProgress, StateCode.Pending, fs.Position.ToString()));
                    }
                    fs.Read(dataMsg.Data, 0, (int)(fs.Length - fs.Position));
                    bs = dataMsg.ToBytes();
                    ns.Write(bs, 0, bs.Length);
                    ns.Flush();
                });
                OnTransferDone(new State(ActionCode.FileSend, StateCode.Success, ""));
            }
            catch (OperationCanceledException)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, status + " Timeout"));
            }
            catch (SocketException e)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, e.Message));
                throw e;
            }
        }
        public void SendText(string addr, string text)
        {
            aSocket?.Cancel();
            Message message = new Message { Text = text };
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(addr), ReceiverPort);
                UdpSend(remoteEP, Message.CreateTextMeta(text.Length));
                remoteEP.Port = TransferPort;
                TcpSetupStream(remoteEP, ns =>
                {
                    byte[] bs = message.ToBytes();
                    ns.Write(bs, 0, bs.Length);
                });
                OnTransferDone(new State(ActionCode.TextSend, StateCode.Success, ""));
            }
            catch (OperationCanceledException)
            {
                OnTransferError?.Invoke(new State(ActionCode.TextSend, StateCode.Error, "Confirm Addr Timeout"));
            }
            catch (SocketException e)
            {
                OnTransferError?.Invoke(new State(ActionCode.TextSend, StateCode.Error, e.Message));
                throw e;
            }
        }
    }
}
