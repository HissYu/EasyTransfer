using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transfer
{
    delegate void MessageRedirect(Redirection msg);
    enum ThreadType
    {
        UdpListen,
    }
    class Receiver : Core
    {
        //public new readonly int PortUsed = ReceiverPort;
        public Receiver(): base(CoreType.Receiver) { }
        CancellationTokenSource backgroundWorkHandler;
        public void ActivateListening()
        {
            IPEndPoint remoteEP = new IPEndPoint(MulticastAddr, SenderPort);
            Message message = null;

            while (true)
            {
                UdpMulticastReceive(ref remoteEP,
                    (msg) => msg.Type == MsgType.Info,
                    (msg) => { message = msg; });
                UdpMulticastSend(new Message { Pin = message.Pin });
                try
                {
                    CallWithTimeout(() =>
                    {
                        UdpReceive(ref remoteEP,
                            (msg) => msg.Type == MsgType.Key,
                            (msg) => message = msg);
                    }, Timeout);
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine("Connection request received, but timeout encountered when getting confirm. ");
                    continue;
                }
                UdpSend(remoteEP, message);
                SaveDevice(message.Key, remoteEP.Address.ToString());
                break;
            }
        }
        public void ActivateBackgroundReceiver()
        {
            //backgroundWorkHandler = CallAtBackground(() =>
            //{
            while (true)
            {
                IPEndPoint remoteEP = new IPEndPoint(MulticastAddr, SenderPort);
                Device device = null;
                string key2 = null;
                bool isText = false; Message meta = null;
                UdpMulticastReceive(ref remoteEP,
                    msg => msg.Type == MsgType.Confirm,
                    msg =>
                    {
                        device = FindKey(msg.SemiKey);
                        if (device != null)
                            key2 = Utils.GenerateGuardKey(device.Key, msg.SemiKey);
                        msg.SemiKey = key2;
                            
                    });
                UdpSend(remoteEP, new Message { SemiKey = key2});
                if (device == null || key2 == "")
                    continue;
                try
                {
                    CallWithTimeout(() =>
                    {
                        UdpReceive(ref remoteEP,
                            msg => msg.Type == MsgType.Confirm && msg.SemiKey == key2,
                            msg =>
                            {
                                device.LastAddr = remoteEP.Address.ToString();
                                SaveDevice(device);
                            });
                    }, Timeout);
                    CallWithTimeout(() =>
                    {
                        UdpReceive(ref remoteEP,
                            msg => msg.Type == MsgType.Meta,
                            msg => { isText = Message.IsText(msg); meta = msg; }
                            );
                    }, Timeout);
                }
                catch (OperationCanceledException)
                {

                    continue;
                }
                if (!isText)
                {
                    ReceiveFile(remoteEP, meta);
                }
                else TcpAcceptStream(ns =>
                    {
                        byte[] bs = new byte[meta.Size + 1];
                        ns.Read(bs, 0, (int)meta.Size + 1);
                        string txt = Message.Parse(bs).Text;
                        Console.WriteLine(txt);
                    });
            }
            //});
            
        }

        private void ReceiveFile(IPEndPoint remoteEP, Message meta)
        {
            int continueId = LoadMetaFile(meta);
            UdpSend(remoteEP, new Message { PackID = continueId });
            TcpAcceptStream(ns =>
            {
                using (FileStream metafs = new FileStream(meta.Filename + ".meta", FileMode.Open))
                using (FileStream fs = new FileStream(meta.Filename, FileMode.OpenOrCreate))
                {
                    Message data;
                    byte[] bs = new byte[meta.PackSize + 9];
                    for (int i = continueId - 1; i < meta.PackCount - 1; i++)
                    {
                        ns.Read(bs, 0, meta.PackSize + 9);
                        data = Message.Parse(bs);
                        fs.Seek(0, SeekOrigin.End);
                        fs.Write(data.Data, 0, meta.PackSize);
                        metafs.Seek(-8, SeekOrigin.End);
                        metafs.Write(Utils.GetBytes(data.PackID+1), 0, 8);
                        fs.Flush(); metafs.Flush();
                    }
                    ns.Read(bs, 0, meta.PackSize + 9);
                    data = Message.Parse(bs);
                    fs.Write(data.Data, 0, (int)(meta.Size - meta.PackSize * (meta.PackCount - 1)));
                    fs.Flush();
                }
                File.Delete(meta.Filename + ".meta");
            });
            Console.WriteLine("Transfer done.");
        }

        private int LoadMetaFile(Message meta)
        {
            if (!File.Exists(meta.Filename+".meta"))
            {
                byte[] bs = new byte[256 + 8];
                Array.Copy(meta.Hash, 0, bs, 0, 256);
                Array.Copy(Utils.GetBytes((long)1), 0, bs, 256, 8);
                File.WriteAllBytes(meta.Filename + ".meta", bs);
                return 1;
            }
            using(FileStream fs = new FileStream(meta.Filename + ".meta",FileMode.Open))
            {
                byte[] hash = new byte[256];
                byte[] id = new byte[8];
                fs.Read(hash, 0, 256);
                if (hash.AsSpan().SequenceEqual(meta.Hash.AsSpan()))
                {
                    fs.Read(id, 0, 8);
                    return (int)Utils.BtoLong(id);
                }
                else throw new Exception("Two files with same name have differed hash, please check your file or rename it.");
            }
        }
        //private void SaveProcess(string f)
    }
}
