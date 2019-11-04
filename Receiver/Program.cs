using System;
using Common;
namespace Transfer
{
    class Program
    {
        static void Main(string[] args)
        {
            Core.OnReceivedRequest += (meta, isText)=>{
                if (isText) return true;

                Console.WriteLine($"New File to Receive:\nFilename: {meta.Filename}\nSize: {Utils.FormatFileSize(meta.PackCount * meta.PackSize)}");
                if (Console.ReadKey().Key == ConsoleKey.Y)
                    return true;
                else return false;
            };
            Core.OnTransferDone += (state) =>
            {
                switch (state.For)
                {
                    case ActionCode.FileReceive:
                        break;
                    case ActionCode.TextReceive:
                        Console.WriteLine($"Text received: =:{state.Data}:=");
                        break;
                    case ActionCode.FileSend:
                        break;
                    case ActionCode.TextSend:
                        break;
                    default:
                        break;
                }
            };
            Receiver receiver = new Receiver();
            receiver.StartAnnouncing();
            receiver.StartWorking();
            //while (true)
            //{
                Console.ReadKey();
            //}
        }
    }
}
