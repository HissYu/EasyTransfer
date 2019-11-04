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
        public Sender() : base(CoreType.Sender) {
            UdpGlobalReceive();
        }

        public async void FindDeviceAroundAsync()
        {
            try
            {
                List<Device> devicesFound = new List<Device>();
                while (true)
                {
                    devicesFound.Clear();

                    TaskCompletionSource<bool> gotMsg = new TaskCompletionSource<bool>();
                    UdpReceived ev = (remote, arg) =>
                    {
                        if (arg.Handled)
                            return;
                        var msg = arg.Mess;
                        if (msg.Type == MsgType.Info && msg.Pin == 100000)
                        {
                            devicesFound.Add(new Device { Addr = remote.Address.ToString(), Name = msg.DeviceName, Time = DateTime.Now });
                            gotMsg.SetResult(true);
                        }
                    };
                    OnUdpReceived += ev;

                    await gotMsg.Task;
                    OnUdpReceived -= ev;
                    
                    OnDeviceFound?.Invoke(devicesFound);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
        public void FindDeviceAround()
        {
            try
            {
                Message MsgSent = new Message();
                List<Device> devicesFound = new List<Device>();
                while (true)
                {
                    devicesFound.Clear();
                    MsgSent.Pin = Utils.GeneratePin();
                    MsgSent.DeviceName = Utils.GetDeviceName();
                    UdpMulticastSend(MsgSent);

                    CallWithTimeout(() =>
                    {
                        while (true)
                        {
                            WaitMessage(
                                (msg) => msg.Type == MsgType.Info && msg.Pin == MsgSent.Pin,
                                (remote, msg) => devicesFound.Add(new Device { Addr = remote.Address.ToString(), Name = msg.DeviceName }));
                        }
                    }, 3000);
                    OnDeviceFound?.Invoke(devicesFound);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
        public void StopBackgroudWork()
        {
            //aSocket.Cancel();
            //aSocket = null;
        }
        public async void SendFileAsync(string addr, string filepath)
        {
            Message meta = Message.CreateMeta(filepath, PackSize);
            string status = "";
            long continueId = 0;
            try
            {
                status = "Wait Confirmation";
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(addr), ReceiverPort);
                UdpSend(remoteEP, meta);

                await WaitMessage(
                    msg => msg.Type == MsgType.Continue,
                    (remoteEP, msg) => continueId = msg.PackID);

                if (continueId == -1)
                {
                    OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, "Request rejected"));
                    return;
                }

                remoteEP.Port = TransferPort;
                status = "Transferring";

                TcpSetupStream(remoteEP, ns =>
                {
                    using FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
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

                        OnPackTransfered?.Invoke((fs.Position / fs.Length));
                    }
                    fs.Read(dataMsg.Data, 0, (int)(fs.Length - fs.Position));
                    bs = dataMsg.ToBytes();
                    ns.Write(bs, 0, bs.Length);
                    ns.Flush();
                });
                OnTransferDone?.Invoke(new State(ActionCode.FileSend, StateCode.Success, ""));
            }
            catch (OperationCanceledException)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, status + " Timeout"));
            }
            catch (SocketException e)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, e.Message));
            }
        }
        public void SendFile(string addr, string filepath)
        {
            //aSocket?.Cancel();
            //string filename = Path.GetFileName(filepath);
            Message meta = Message.CreateMeta(filepath, PackSize);
            string status = "";
            long continueId = 0;
            try
            {
                status = "Wait Confirmation";
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(addr),ReceiverPort);
                UdpSend(remoteEP, meta);

                remoteEP.Address = IPAddress.Any;
                UdpReceive(ref remoteEP,
                    msg => msg.Type == MsgType.Continue,
                    msg => continueId = msg.PackID);

                if (continueId == -1)
                {
                    OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, "Request rejected"));
                    return;
                }

                remoteEP.Port = TransferPort;
                status = "Transferring";

                TcpSetupStream(remoteEP, ns =>
                {
                    using FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
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

                        OnPackTransfered?.Invoke((fs.Position/fs.Length));
                    }
                    fs.Read(dataMsg.Data, 0, (int)(fs.Length - fs.Position));
                    bs = dataMsg.ToBytes();
                    ns.Write(bs, 0, bs.Length);
                    ns.Flush();
                });
                OnTransferDone?.Invoke(new State(ActionCode.FileSend, StateCode.Success, ""));
            }
            catch (OperationCanceledException)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, status + " Timeout"));
            }
            catch (SocketException e)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileSend, StateCode.Error, e.Message));
            }
        }
        public async void SendTextAsync(string addr,string text)
        {
            await Task.Run(() => SendText(addr, text));
        }
        public void SendText(string addr, string text)
        {
            //aSocket?.Cancel();
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
                OnTransferDone?.Invoke(new State(ActionCode.TextSend, StateCode.Success, ""));
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
