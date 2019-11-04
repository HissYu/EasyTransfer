using System;
using System.Collections.Generic;
using System.Net;

namespace Common
{
    public delegate bool NewTransferEvent(Message meta, bool isText);
    public delegate void ProgressPush(double progress);
    public delegate void StatusEvent(State newState);
    public delegate void DeviceFoundEvent(List<Device> devices);

    public class UdpReceivedArg
    {
        public bool Handled { get; set; }
        public Message Mess { get; set; }
    }
    public delegate void UdpReceived(IPEndPoint remote, UdpReceivedArg arg);
    
    

    public class Device
    {
        public string Name { get; set; }
        public string Addr { get; set; }
        public DateTime Time { get; set; }
    }
    public enum StateCode
    {
        Error, Success, Pending
    }
    public enum ActionCode
    {
        DeviceFound,
        FileReceive,
        TextReceive,
        FileCheck,
        FilePackProgress,
        FileSend,
        TextSend
    }
    public class State
    {
        public State(ActionCode action, StateCode state, string data)
        {
            For = action;
            What = state;
            Data = data;
        }
        public ActionCode For { get; }
        public StateCode What { get; }
        public string Data { get; }
    }
    
}
