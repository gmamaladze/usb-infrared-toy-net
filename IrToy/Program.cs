using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IrToy1
{

    class Program
    {

        private static string Execute(SerialPort port, string command)
        {
            port.Write(command);
            Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
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

            var portName = ports[0];
            using (var port = new SerialPort(portName))
            {
                port.Open();

                var init = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 };
                port.Write(init, 0, init.Length);

                Console.WriteLine($"Version: {Execute(port, "v")}");
                Console.WriteLine($"Test: {Execute(port, "t")}");
                Console.WriteLine($"Handshake: {Execute(port, "r")}");
                Console.WriteLine($"Enter sampling mode: {Execute(port, "s")}");
                port.Write(new byte[] { 0x24, 0x25, 0x26 }, 0, 3); // Enable advanced TX features (no reply)
                Console.WriteLine("---------------------------------");
                Console.WriteLine("Recording....");
                var endOfSiganl = false;
                var stack = new Stack<byte>();
                while (!endOfSiganl)
                {
                    var received = port.ReadByte();
                    if (received != -1)
                    {
                        endOfSiganl = (received == 0xFF) && (stack.Peek() == 0xFF);
                        stack.Push((byte)received);
                    }
                }
                stack.Pop();
                stack.Pop();
                var mesage = stack.ToArray();
                Console.WriteLine($"Recorded {mesage.Length} bytes.");


                Console.WriteLine("Pess any key to send. Press ESC to quit.");


                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                    return;

                Console.WriteLine("---------------------------------");
                port.Write(new byte[] { 0x03 }, 0, 1); //start

                int offset = 0;
                while (true)
                {
                    var bufferSize = port.ReadByte();
                    var count = Math.Min(bufferSize, mesage.Length - offset);
                    port.Write(mesage, offset, count);
                    offset += count;
                    Console.WriteLine($"Sent {count} bytes.");
                    if (offset >= mesage.Length)
                        break;
                }
                port.Write(new byte[] { 0xFF, 0xFF }, 0, 2); //End of message
                var high = port.ReadByte();
                var low = port.ReadByte();
                var transmittedBytes = high + low << 8;
                Console.WriteLine($"Confirmed {transmittedBytes} bytes.");
                var end = port.ReadExisting();
                Console.WriteLine($"End {end}");
                port.Close();
            }
        }
    }
}