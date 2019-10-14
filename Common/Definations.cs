namespace Common
{
    public delegate int InitailizeProgress();
    public delegate void UpdateProgress(int progress);
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
        public string Key { get; set; }
        public string LastAddr { get; set; }
    }
    public enum StateCode
    {
        Error, Success, Pending
    }
    public enum ActionCode
    {
        Accept,
        FileReceive,
        TextReceive,
        FileCheck,
        Connect,
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
