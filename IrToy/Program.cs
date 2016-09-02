using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IrToy
{
    class Program
    {

        private static string Execute(SerialPort port, string command)
        {
            port.Write(command);
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            var response = port.ReadExisting();
            return response;
        }

        static void Main(string[] args)
        {
            var ports = SerialPort.GetPortNames();
            for (int i = 0; i < ports.Length; i++)
            {
                Console.WriteLine($"[{i+1}] {ports[i]}");
            }
            Console.WriteLine("Press port number or any other key to exit.");
            var pressed = Console.ReadKey(true);
            Console.WriteLine();
            var ch = pressed.KeyChar;
            int index;
            var isOk = int.TryParse(ch.ToString(), out index);
            if (!isOk && index>ports.Length) return;

            var portName = ports[index-1];
            using (var port = new SerialPort(portName, 115200))
            {
                port.Open();

                var init = new byte[] { 0xFF, 0x00 };
                port.Write(init, 0, 1);
                port.Write(init, 0, 1);
                port.Write(init, 1, 1);
                port.Write(init, 1, 1);
                port.Write(init, 1, 1);
                port.Write(init, 1, 1);
                port.Write(init, 1, 1);

                Console.WriteLine($"Version: {Execute(port, "v")}");
                Console.WriteLine($"Test: {Execute(port, "t")}");
                Console.WriteLine($"Protocol: {Execute(port, "S")}");

                var queue = new ConcurrentQueue<byte>();
                using (var ctx = new CancellationTokenSource())
                {
                    var receiver = Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            var received = port.ReadByte();
                            if (received != -1)
                            {
                                queue.Enqueue((byte)received);
                                Console.Write(received);
                            }
                        }
                    }, ctx.Token);

                    ConsoleKey key = ConsoleKey.NoName;
                    while (key != ConsoleKey.Q)
                    {
                        key = Console.ReadKey(true).Key;
                        switch (key)
                        {
                            case ConsoleKey.R:
                                while (queue.Count > 0)
                                {
                                    byte _;
                                    queue.TryDequeue(out _);
                                }
                                break;
                            case ConsoleKey.P:
                                Console.WriteLine();
                                Console.WriteLine("Sending ...");
                                var buffer = queue.ToArray();
                                port.Write(init, 1, 1);
                                port.Write(new byte[] { 0x03 }, 0, 1);
                                port.Write(buffer, 0, buffer.Length);
                                break;
                        }
                    }
                }
                port.Close();
            }
        }
    }
}
