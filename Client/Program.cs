using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    
    class Program
    {
        private static readonly Socket _socket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int _port = 65001;

        private static void connect()
        {
            while (!_socket.Connected)
            {
                try
                {
                    _socket.Connect(IPAddress.Loopback, _port);
                }
                catch(SocketException e)
                {
                    Console.WriteLine("Exception" + e.InnerException.Message);
                }
            }
            runLoop();
        }

        private static void runLoop()
        {
            while(true)
            {
                sendData();
                recieveData();
            }
        }

        private static void recieveData()
        {
            byte[] data = new byte[1024];
            int length = _socket.Receive(data, SocketFlags.None);
            byte[] trimmed = new byte[length];
            Array.Copy(data, trimmed, length);
            Console.WriteLine(Encoding.ASCII.GetString(trimmed));
        }

        private static void sendData()
        {
            byte[] data = Encoding.ASCII.GetBytes("print time");
            _socket.Send(data, 0, data.Length, SocketFlags.None);
        }

        static void Main(string[] args)
        {
            connect();
            Console.ReadLine();
        }
    }
}
