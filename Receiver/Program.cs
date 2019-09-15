using System;

namespace Transfer
{
    class Program
    {
        static void Main(string[] args)
        {
            Receiver receiver = new Receiver();
            switch (args[0])
            {
                case "-a":
                    receiver.ActivateListening();
                    break;
                case "":
                    break;
            }
        }
    }
}
