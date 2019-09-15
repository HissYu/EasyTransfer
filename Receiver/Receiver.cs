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
        public void ActivateListening()
        {
            IPAddress localAddr = IPAddress.Parse(Utils.GetLocalIPAddress());
            IPAddress remoteAddr = null;
            Message message = null;

            while (true)
            {
                UdpMulticastReceive((msg) => msg.Type == MsgType.Info,
                    (msg) => { remoteAddr = msg.IP; message = msg; });
                UdpMulticastSend(new Message { IP = localAddr, Pin = message.Pin });
                try
                {
                    CallWithTimeout(() =>
                    {
                        UdpReceive(new IPEndPoint(remoteAddr, OutPort),
                            (msg) => msg.Type == MsgType.Key,
                            (msg) => message = msg);
                    }, Timeout);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Connection request received, but timeout encountered when getting confirm. ");
                    continue;
                }
                UdpSend(new IPEndPoint(remoteAddr, InPort), message);
                SaveDevice(message.Key, remoteAddr.ToString());
                break;
            }
        }
    }
}
