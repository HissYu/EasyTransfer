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
    public class Redirection
    {
        public bool Handled = false;
        public Message Message = null;
        public Redirection(Message msg)
        {
            Message = msg;
        }
    }
    public class Core
    {
        protected int InPort = 37384;
        protected int OutPort = 38384;
        protected int MulticastPort = 37484;
        protected readonly IPAddress MulticastAddr = IPAddress.Parse("234.2.3.4");

        protected void CallWithTimeout(Action action, int miliseconds)
        {
            Task wrapper = new Task(() => action());
            using (CancellationTokenSource cancellation = new CancellationTokenSource(miliseconds))
            {
                wrapper.Start();
                wrapper.Wait(cancellation.Token);
            }
        }
        protected CancellationTokenSource CallAtBackground(Action action)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Task wrapper = new Task(() => action(), cancellation.Token);
            wrapper.Start();
            return cancellation;
        }
        protected void UdpSend(IPEndPoint remoteEP, Message msg)
        {
            byte[] bs = msg.ToBytes();
            using(UdpClient client = new UdpClient(OutPort))
            {
                client.Send(bs, bs.Length, remoteEP);
                //DEBUG
                Console.WriteLine("Message:\n{0}\nSent to {1}:{2}",msg,remoteEP.Address.ToString(),remoteEP.Port);
            }
        }
        protected void UdpReceive(IPEndPoint remoteEP, Func<Message, bool> condition, Action<Message> callback)
        {
            while (true)
            {
                using(UdpClient client = new UdpClient(InPort))
                {
                    byte[] receive = client.Receive(ref remoteEP);
                    if (Message.TryParse(receive,out Message rMessage)&&condition(rMessage))
                    {
                        //DEBUG
                        Console.WriteLine("Message:\n{0}\nReceive from {1}:{2}", rMessage, remoteEP.Address.ToString(), remoteEP.Port);
                        callback(rMessage);
                        return;
                    }
                }
            }
        }
        protected void UdpMulticastSend(Message message)
        {
            IPEndPoint multicastEP = new IPEndPoint(MulticastAddr, InPort);
            UdpSend(multicastEP, message);
        }
        protected void UdpMulticastReceive(Func<Message, bool> condition, Action<Message> callback)
        {
            while (true)
            {
                using (UdpClient client = new UdpClient(InPort))
                {
                    client.JoinMulticastGroup(MulticastAddr);
                    IPEndPoint multicastEP = new IPEndPoint(MulticastAddr, OutPort);
                    byte[] receive = client.Receive(ref multicastEP);
                    if (Message.TryParse(receive, out Message rMessage) && condition(rMessage))
                    {
                        //DEBUG
                        Console.WriteLine("Message:\n{0}\nReceive from {1}:{2}", rMessage, multicastEP.Address.ToString(), multicastEP.Port);
                        callback(rMessage);
                        return;
                    }
                }
            }
        }
        protected void SaveDevice(string key, string lastAddr)
        {
            using (StreamWriter sw = new StreamWriter("devices"))
            {
                Console.WriteLine("Device name: ");
                string name = Console.ReadLine();
                sw.Write($"{(name == "" ? "unamed device" : name)}"+','+lastAddr+','+$@"{key}"+'\n');
            }
        }
    }
}
