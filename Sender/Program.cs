using System;


namespace Transfer
{
    class Program
    {
        static void Main(string[] args)
        {
            Sender sender = new Sender();
            switch (args[0])
            {
                case "-s":
                    sender.AddDeviceFromScanning();
                    break;
                default:
                    break;
            }
        }
    }
}
