using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Common
{
    delegate void MessageRedirect(Redirection msg);

    [Serializable]
    public class ChecksumMismatchException : Exception
    {
        public ChecksumMismatchException() { }
        public ChecksumMismatchException(string message) : base(message) { }
        public ChecksumMismatchException(string message, Exception inner) : base(message, inner) { }
        protected ChecksumMismatchException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    enum ThreadType
    {
        UdpListen,
    }
    public class Receiver : Core
    {
        public Receiver() : base(CoreType.Receiver) { }
        //CancellationTokenSource backgroundWorkHandler;
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
                    //Console.Error.WriteLine("Connection request received, but timeout encountered when getting confirm. ");
                    UpdateState?.Invoke(new State(ActionCode.Accept, StateCode.Error, "Connection request received, but timeout encountered when getting confirm."));
                    continue;
                }
                UdpSend(remoteEP, message);
                //SaveDevice(message.Key, remoteEP.Address.ToString()); // export ??
                UpdateState?.Invoke(new State(ActionCode.Accept, StateCode.Success, message.Key+","+remoteEP.Address.ToString()));
                break;
            }
        }
        public void ActivateReceiver()
        {
            while (true)
            {
            begin:
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
                UdpSend(remoteEP, new Message { SemiKey = key2 });
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

                    if (!isText)
                    {
                        ReceiveFile(remoteEP, meta);
                        UpdateState?.Invoke(new State(ActionCode.FileReceive, StateCode.Success, meta.Filename));
                    }
                    else TcpAcceptStream(ns =>
                    {
                        byte[] bs = new byte[meta.Size + 1];
                        ns.Read(bs, 0, (int)meta.Size + 1);
                        string txt = Message.Parse(bs).Text;
                        //Console.WriteLine(txt);
                        UpdateState?.Invoke(new State(ActionCode.TextReceive, StateCode.Success, txt));
                    });
                }
                catch (OperationCanceledException)
                {
                    goto begin;
                }
                catch (ChecksumMismatchException e)
                {
                    UpdateState?.Invoke(new State(ActionCode.FileCheck, StateCode.Error, e.Message));
                }
            }
        }

        private void ReceiveFile(IPEndPoint remoteEP, Message meta)
        {
            int continueId = LoadMetaFile(meta);
            UdpSend(remoteEP, new Message { PackID = continueId });
            TcpAcceptStream(ns =>
            {
                using (FileStream metafs = new FileStream(meta.Filename + ".meta", FileMode.Open))
                using (FileStream fs = new FileStream(meta.Filename, FileMode.OpenOrCreate))
                using (SHA256 sha = SHA256.Create())
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
                        metafs.Write(Utils.GetBytes(data.PackID + 1), 0, 8);
                        fs.Flush(); metafs.Flush();
                    }
                    ns.Read(bs, 0, meta.PackSize + 9);
                    data = Message.Parse(bs);
                    fs.Write(data.Data, 0, (int)(meta.Size - meta.PackSize * (meta.PackCount - 1)));
                    fs.Flush();

                    fs.Seek(0, SeekOrigin.Begin);
                    metafs.Seek(0, SeekOrigin.Begin);
                    byte[] metahs = new byte[256];
                    metafs.Read(metahs, 0, 256);
                    byte[] hash = new byte[256];
                    byte[] t = sha.ComputeHash(fs);
                    Array.Copy(t, 0, hash, 0, t.Length);
                    if (!hash.SequenceEqual(metahs))
                    {
                        //Console.WriteLine("Checksum not match, please resend the file.");
                        throw new ChecksumMismatchException("Checksum not match, please resend the file.");
                    }
                }
                File.Delete(meta.Filename + ".meta");
            });
            //Console.WriteLine("Transfer done.");
        }

        private int LoadMetaFile(Message meta)
        {
            if (!File.Exists(meta.Filename + ".meta"))
            {
                byte[] bs = new byte[256 + 8];
                Array.Copy(meta.Hash, 0, bs, 0, 256);
                Array.Copy(Utils.GetBytes((long)1), 0, bs, 256, 8);
                File.WriteAllBytes(meta.Filename + ".meta", bs);
                return 1;
            }
            using FileStream fs = new FileStream(meta.Filename + ".meta", FileMode.Open);
            byte[] hash = new byte[256];
            byte[] id = new byte[8];
            fs.Read(hash, 0, 256);
            if (hash.SequenceEqual(meta.Hash))
            {
                fs.Read(id, 0, 8);
                return (int)Utils.BtoLong(id);
            }
            else throw new ChecksumMismatchException("Two files with same name have differed hash, please check your file or rename it.");
        }
        //private void SaveProcess(string f)
    }
}
