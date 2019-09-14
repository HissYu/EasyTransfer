using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Transfer
{
    class Sender : Core
    {
        //protected new const int Port = 37384;
        //protected readonly UdpClient UdpClient = new UdpClient();

        //public void UdpSendTo(IPAddress addr, byte[] content)
        //{
        //    if (content is null)
        //    {
        //        throw new ArgumentNullException(nameof(content));
        //    }
        //    UdpClient.Send(content, content.Length, new IPEndPoint(addr, InPort));

        //}
        //public void UdpSendMulticast(byte[] content) => UdpSendTo(MulticastAddr, content);
        //public void TcpSendStream()
        //{

        //}
        
        public void AddDeviceFromScanning()
        {
            IPAddress localAddr = IPAddress.Parse(Utils.GetLocalIPAddress());
            IPAddress remoteAddr = null;
            Message MsgSent = new Message { IP = localAddr, Pin = Utils.GeneratePin() };
            string status = null;
            bool confirmed = false;

            try
            {
                UdpMulticastSend(MsgSent);
                CallWithTimeout(()=> {
                    status = "Get Response";
                    UdpMulticastReceive((msg) => msg.Type == MsgType.Info && msg.Pin == MsgSent.Pin,
                        (msg) => { remoteAddr = msg.IP; }
                        );
                }, 10000);

                MsgSent = new Message { Key = Utils.GenerateKey() };
                UdpSend(new IPEndPoint(remoteAddr, InPort), MsgSent);
                
                CallWithTimeout(()=> {
                    status = "Get Confirm";
                    UdpReceive(new IPEndPoint(remoteAddr, OutPort),
                        (msg) => msg.Type == MsgType.Key && msg.Key == MsgSent.Key,
                        (msg) => { confirmed = true; });
                }, 10000);

                if (confirmed)
                {
                    SaveDevice(MsgSent.Key, remoteAddr.ToString());
                }
            }
            catch (OperationCanceledException)
            {
                remoteAddr = null;
                Console.Error.WriteLine(status + " Timeout");
            }
            
        }
    }
}
