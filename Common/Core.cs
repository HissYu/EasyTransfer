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
    public enum CoreType
    {
        Sender, Receiver
    }
    public class Core
    {
        protected const int ReceiverPort = 37384;
        protected const int SenderPort = 38384;
        protected const int TransferPort = 37484;
        public readonly int PortUsed;
        public readonly int PortUnused;
        private readonly UdpClient client;
        protected const int Timeout = 5000;
        protected readonly IPAddress MulticastAddr = IPAddress.Parse("234.2.3.4");
        protected readonly IPAddress LocalAddr = IPAddress.Parse(Utils.GetLocalIPAddress());

        static string deviceListFile = "devices";

        //public static Action<State> UpdateState;
#nullable enable
        public static NewTransferEvent? OnReceivedRequest;
        public static StatusEvent? OnTransferDone;
        public static StatusEvent? OnPackTransfered;
        public static StatusEvent? OnTransferError;
        public static DeviceFoundEvent? OnDeviceFound;
#nullable restore
        public void OnAndroidDevice()
        {
            // Maybe use this directory on every platform
            deviceListFile = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "devices");
        }
        public Core(CoreType t)
        {
            switch (t)
            {
                case CoreType.Sender:
                    PortUsed = SenderPort;
                    PortUnused = ReceiverPort;
                    break;
                case CoreType.Receiver:
                    PortUsed = ReceiverPort;
                    PortUnused = SenderPort;
                    break;
                default:
                    throw new Exception();
            }
            client = new UdpClient(PortUsed);
            client.JoinMulticastGroup(MulticastAddr);
        }
        protected void CallWithTimeout(Action action, int miliseconds)
        {
            Task wrapper = new Task(() => action());
            using CancellationTokenSource cancellation = new CancellationTokenSource(miliseconds);
            wrapper.Start();
            wrapper.Wait(cancellation.Token);
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
            
            client.Send(bs, bs.Length, remoteEP);
            
        }
        protected void UdpReceive(ref IPEndPoint remoteEP, Predicate<Message> condition, Action<Message> callback)
        {
            while (true)
            {
                Console.WriteLine("Start listening at {0}:{1}", remoteEP.Address, remoteEP.Port);

                byte[] receive = client.Receive(ref remoteEP);

                Console.WriteLine("Received: {0}", Utils.ShowBytes(receive));
                if (Message.TryParse(receive, out Message rMessage) && condition(rMessage))
                {
                    callback(rMessage);
                    return;
                }
            }
        }
        protected void UdpMulticastSend(Message message)
        {
            IPEndPoint multicastEP = new IPEndPoint(MulticastAddr, PortUnused);
            UdpSend(multicastEP, message);
        }
        protected void UdpMulticastReceive(ref IPEndPoint remoteEP, Predicate<Message> condition, Action<Message> callback)
        {
            while (true)
            {
                Console.WriteLine("Start listening at {0}:{1}", remoteEP.Address, remoteEP.Port);
                byte[] receive = client.Receive(ref remoteEP);

                if (Message.TryParse(receive, out Message rMessage) && condition(rMessage))
                {
                    callback(rMessage);
                    return;
                }
            }
        }
        protected void TcpSetupStream(IPEndPoint remoteEP, Action<NetworkStream> streamAction)
        {
            using TcpClient client = new TcpClient();
            client.Connect(remoteEP);
            if (client.Connected)
            {
                var ns = client.GetStream();
                streamAction(ns);
                ns.Close(); client.Close();
            }
        }
        protected void TcpAcceptStream(Action<NetworkStream> streamAction)
        {
            TcpListener listener = new TcpListener(LocalAddr, TransferPort);
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();
            var ns = client.GetStream();
            streamAction(ns);
            ns.Close();
            listener.Stop();
        }
    }

}
