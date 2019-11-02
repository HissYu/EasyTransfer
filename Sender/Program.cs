using System;
using System.IO;
using System.Collections.Generic;
using Common;
using System.Threading.Tasks;
using System.Threading;

namespace Transfer
{
    class Program
    {
        static CancellationTokenSource current = new CancellationTokenSource();
        static void Main(string[] args)
        {
            Sender sender = new Sender(ref current);
            Core.OnDeviceFound += (devices) =>
            {
                (devices as List<Device>).ForEach((e) =>
                {
                    global::System.Console.WriteLine(e.Name + ' ' + e.Addr);
                });
            };
            sender.FindDeviceAroundAsync();
            while (true)
            {
                Console.ReadKey();
            }
        }
    }
}
