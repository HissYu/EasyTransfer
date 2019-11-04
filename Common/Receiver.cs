using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{

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
        

        public async void StartAnnouncing()
        {
            Message announcement = new Message { DeviceName = Utils.GetDeviceName(), Pin = 100000 };
            await Task.Run(() =>
            {
                while (true)
                {
                    UdpMulticastSend(announcement);
                    Thread.Sleep(2000);
                }
            }).ConfigureAwait(false);
        }
        public void StartWorking()
        {
            try
            {
                while (true)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, SenderPort);
                    Message meta = null;
                    bool isText = false;
                    UdpReceive(ref remoteEP,
                        msg => { Console.WriteLine(msg); return msg.Type != MsgType.Invalid; },
                        msg =>
                        {
                            if (msg.Type == MsgType.Meta)
                            {
                                if (OnReceivedRequest(msg, Message.IsText(msg)))
                                {
                                    meta = msg;
                                    isText = Message.IsText(msg);
                                }
                                else
                                {
                                    UdpSend(remoteEP, new Message { PackID = -1 });
                                }
                            }
                        });
                    if (meta != null)
                    {
                        if (!isText)
                        {
                            ReceiveFile(remoteEP, meta);
                            OnTransferDone?.Invoke(new State(ActionCode.FileReceive, StateCode.Success, meta.Filename));
                        }
                        else TcpAcceptStream(ns =>
                        {
                            byte[] bs = new byte[meta.Size + 1];
                            ns.Read(bs, 0, (int)meta.Size + 1);
                            string txt = Message.Parse(bs).Text;
                                //Console.WriteLine(txt);
                                OnTransferDone?.Invoke(new State(ActionCode.TextReceive, StateCode.Success, txt));
                        });
                    }
                }
            }
            catch (ChecksumMismatchException e)
            {
                OnTransferError?.Invoke(new State(ActionCode.FileCheck, StateCode.Error, e.Message));
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

                        OnPackTransfered?.Invoke((i / meta.PackCount));
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
                        throw new ChecksumMismatchException("Checksum not match, file may be collapsed.");
                    }
                }
                File.Delete(meta.Filename + ".meta");
            });
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
    }
}
