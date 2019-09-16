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
        new readonly int PortUsed = ReceiverPort;
        public void ActivateListening()
        {
            IPAddress localAddr = IPAddress.Parse(Utils.GetLocalIPAddress());
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
    }
}
