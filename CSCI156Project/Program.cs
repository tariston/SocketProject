using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
        private const int _bufferSize = 1024;
        private static readonly byte[] _buffer = new byte[_bufferSize];
        private const int _port = 65001;
        private static List<Socket> _sockets = new List<Socket>();
        private static List<SocketHost> _clients = new List<SocketHost>();
        public static List<AuctionItems> auctions = new List<AuctionItems>();

        private static void ServerInit()
        {
            Console.WriteLine("Server initializing");
            for (int i = 0; i < 5; i++)
            {
                var rnd = new Random();
                var auction = new AuctionItems(rnd.Next(100));
                auctions.Add(auction);
            }
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _serverSocket.Listen(3);
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
            _clients.Add(new SocketHost(remoteSocket));
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
                _clients.RemoveAll(x => x.sock.Equals(clientSocket));
                return;
            }
            byte[] temp = new byte[dataSize];
            Array.Copy(_buffer, temp, dataSize);
            string data = Encoding.ASCII.GetString(temp);
            //Logic for bidding
            if (data.ToLower().StartsWith("hostname"))
            {
                string pattern = @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
                Regex rgx = new Regex(pattern);
                string trimmed = rgx.Replace(data, pattern);
                //set the hostname for these sockets
                (from x in _clients where x.sock.Equals(clientSocket) select x).ToList().ForEach(y=>y.hostname = trimmed);

            }
            if (data.ToLower().StartsWith(""))
            {

            }

            //End Logic
            clientSocket.BeginReceive(_buffer, 0, _bufferSize, SocketFlags.None, AsyncRecieveCallback, clientSocket);
        }

        static void Main(string[] args)
        {
            ServerInit();
            Console.ReadLine();
        }
    }

    public class AuctionItems
    {
        public int currentBid 
        { 
            get; 
            set 
            {
                if (value <= 0)
                    return;
                else if (value < _maxValue && _active)
                    currentBid = value;
                else
                {
                    _active = false;
                }
            } 
        }
        public readonly int maxValue { get { return _maxValue; } }
        public readonly Guid pid { get { return _pid; } }
        public readonly bool active { get { return _active; } }

        private Guid _pid { get; set; }
        private int _maxValue;
        private bool _active;


        public AuctionItems(int maxValue)
        {
            this._maxValue = maxValue;
            this._pid = new Guid();
            this.currentBid = 0;
            this._active = true;
        }
    }
    public class SocketHost
    {
        public Socket sock { get; set; }
        public string hostname { get; set; }
        public SocketHost(Socket sock, string hostname = "")
        {
            this.hostname = hostname;
            this.sock = sock;
        }
    }
}
