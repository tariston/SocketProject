using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CSCI156Project
{
    class Program
    {
        private static Socket _serverSocket = new Socket
            (AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
        private const int _bufferSize = 1024;
        private static readonly byte[] _buffer = new byte[_bufferSize];
        private const int _port = 65001;
        private static List<Socket> _sockets = new List<Socket>();

        private static void ServerInit()
        {
            Console.WriteLine("Server initializing");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _serverSocket.Listen(10);
            _serverSocket.BeginAccept(AsyncAcceptCallback, null);
            Console.WriteLine("Server Listening");
        }

        private static void AsyncAcceptCallback(IAsyncResult ar)
        {
            Socket remoteSocket;
            try
            {
                remoteSocket = _serverSocket.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            Console.WriteLine("Client connected");
            _sockets.Add(remoteSocket);
            remoteSocket.BeginReceive(_buffer, 0, _bufferSize, SocketFlags.None, AsyncRecieveCallback, remoteSocket);
            _serverSocket.BeginAccept(AsyncAcceptCallback, null);
        }

        private static void AsyncRecieveCallback(IAsyncResult ar)
        {
            Socket clientSocket = (Socket)ar.AsyncState;
            int dataSize;
            try
            {
                dataSize = clientSocket.EndReceive(ar);
            }
            catch (SocketException)
            {
                clientSocket.Close();
                _sockets.Remove(clientSocket);
                return;
            }
            byte[] temp = new byte[dataSize];
            Array.Copy(_buffer, temp, dataSize);
            string data = Encoding.ASCII.GetString(temp);
            //Logic for bidding

            //End Logic
            clientSocket.BeginReceive(_buffer, 0, _bufferSize, SocketFlags.None, AsyncRecieveCallback, clientSocket);
        }

        static void Main(string[] args)
        {
            ServerInit();
            Console.ReadLine();
        }
    }
}
