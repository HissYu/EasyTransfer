using System;
using System.IO;

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
                case "-l":
                    sender.ListDevices();
                    break;
                case "-f":
                    if (args[1]=="" || args[2]=="")
                        throw new ArgumentNullException("Destination and filename cannot be empty. ");
                    if (File.Exists(args[2]))
                        throw new ArgumentException("File not exist");

                    break;
                case "":
                    break;
            }
        }
    }
}
