using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Transfer
{
    delegate void MessageRedirect(Redirection msg);
    enum ThreadType
    {
        UdpListen,
    }
    class Receiver : Core
    {
        //private readonly UdpClient UdpClient = new UdpClient();

        //MessageRedirect RedirectList;
        //IDictionary<ThreadType, Thread> BackgroundThreads;

        //receiver
        //public void UdpListenBackgroud(IPAddress addr)
        //{
        //    //Thread background = null;
        //    Action listener = () =>
        //    {
        //        BackgroundThreads.Add(ThreadType.UdpListen, Thread.CurrentThread);
        //        IPEndPoint listenEP = new IPEndPoint(addr, OutPort);
        //        while (true)
        //        {
        //            byte[] rec = UdpClient.Receive(ref listenEP);
        //            if (Message.TryParse(rec, out Message res))
        //            {
        //                RedirectList(new Redirection(res));
        //            }
        //        }
        //    };
        //    listener.BeginInvoke(null, null);
        //}

        public void ActivateListening()
        {
            IPAddress localAddr = IPAddress.Parse(Utils.GetLocalIPAddress());
            IPAddress remoteAddr = null;
            Message message = null;

            try
            {
                UdpMulticastReceive((msg) => msg.Type == MsgType.Info,
                (msg) => { remoteAddr = msg.IP; message = msg; });
                Thread.Sleep(10);
                UdpMulticastSend(new Message { IP = localAddr, Pin = message.Pin });
                UdpReceive(new IPEndPoint(remoteAddr, OutPort),
                    (msg) => msg.Type == MsgType.Key,
                    (msg) => { message = msg; });
                Thread.Sleep(500);
                UdpSend(new IPEndPoint(remoteAddr, InPort), message);
                SaveDevice(message.Key, remoteAddr.ToString());
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.ErrorCode);
                Console.WriteLine(e.SocketErrorCode);
                throw e;
            }
        }
    }
}
