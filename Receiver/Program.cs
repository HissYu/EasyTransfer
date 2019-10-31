using System;
using Common;
namespace Transfer
{
    class Program
    {
        static void Main(string[] args)
        {
            Receiver receiver = new Receiver();
            receiver.StartWorking();
            Console.ReadKey();
        }
    }
}
