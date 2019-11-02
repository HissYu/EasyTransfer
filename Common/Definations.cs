using System.Collections.Generic;

namespace Common
{
    public delegate bool NewTransferEvent(Message meta, bool isText);
    public delegate void ProgressPush(double progress);
    public delegate void StatusEvent(State newState);
    public delegate void DeviceFoundEvent(List<Device> devices);
    public class Redirection
    {
        public bool Handled = false;
        public Message Message = null;
        public Redirection(Message msg)
        {
            Message = msg;
        }
    }
    public class Device
    {
        public string Name { get; set; }
        public string Addr { get; set; }
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
        public readonly ActionCode For;
        public readonly StateCode What;
        public readonly string Data;
    }
    
}
