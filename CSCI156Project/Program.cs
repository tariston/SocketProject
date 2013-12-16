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
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int _bufferSize = 1024;
        private static readonly byte[] _buffer = new byte[_bufferSize];
        private const int _port = 65001;
        //private static List<Socket> _sockets = new List<Socket>();
        private static List<SocketHost> _clients = new List<SocketHost>();
        public static List<AuctionItems> auctions = new List<AuctionItems>();

        private static void ServerInit()
        {
            Console.WriteLine("Server initializing");
            for (int i = 0; i < 5; i++)
            {
                var rnd = new Random();
                var auction = new AuctionItems(rnd.Next(100));
                Console.WriteLine("Auction Item" + i);
                Console.WriteLine(auction.active);
                Console.WriteLine(auction.currentBid);
                Console.WriteLine(auction.maxValue);
                Console.WriteLine(auction.pid);
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
            Console.WriteLine("Number of clients connected: " + _clients.Count());
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
            string dataFromClient = Encoding.ASCII.GetString(temp);
            //Logic for bidding
            if (dataFromClient.ToLower().StartsWith("hostname"))
            {
                string UID_pattern = @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
                Regex rgx = new Regex(UID_pattern);
                var match = rgx.Match(dataFromClient);
                var trimmed = match.Value;
                //set the hostname for these sockets
                Console.WriteLine("Setting Client Hostname");
                (from x in _clients where x.sock.Equals(clientSocket) select x).ToList().ForEach(y => y.hostname = trimmed);
                var dataToClient_s = "set";
                var dataToClient = Encoding.ASCII.GetBytes(dataToClient_s);
                clientSocket.Send(dataToClient);
            }
            if (dataFromClient.ToLower().StartsWith("list"))
            {
                Console.WriteLine("Command Received: list");
                var activeAuctions = auctions.Where(x => x.active);
                string raw = String.Join(",", auctions.Select(x => x.ToString()).ToArray());
                byte[] dataToClient = Encoding.ASCII.GetBytes(raw);
                clientSocket.Send(dataToClient);
            }
            if (dataFromClient.ToLower().StartsWith("price"))
            {
                Console.WriteLine("Command Received: price");
                string UID_pattern = @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
                Regex rgx = new Regex(UID_pattern);
                var match = rgx.Match(dataFromClient);
                var trimmed = match.Value;
                var auctionItem = auctions.Find(x => x.pid == new Guid(trimmed));
                byte[] dataToClient;
                if (auctionItem.active)
                {
                    var dataToClient_s = auctionItem.currentBid.ToString();
                    dataToClient = Encoding.ASCII.GetBytes(dataToClient_s);
                }
                else
                {
                    dataToClient = Encoding.ASCII.GetBytes("expired");
                }
                clientSocket.Send(dataToClient);
            }
            if (dataFromClient.ToLower().StartsWith("bid"))
            {
                Console.WriteLine("incoming:" + dataFromClient);
                string Auction_pattern = @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}\s[0-9]+";
                Regex rgx = new Regex(Auction_pattern);
                var match = rgx.Match(dataFromClient);
                var trimmed = match.Value;
                Console.WriteLine("parsed: " + trimmed);
                var split = trimmed.Split(' ');
                var item = split[0];
                var bid_s = split[1];
                var auctionItem = auctions.Find(x => x.pid == new Guid(item));
                if (auctionItem.active)
                {
                    byte[] dataToClient;
                    var bid = Convert.ToInt32(bid_s);
                    if (bid > auctionItem.currentBid)
                    {
                        auctionItem.currentBid = bid;
                        dataToClient = Encoding.ASCII.GetBytes("accepted");
                    }
                    else
                    {
                        dataToClient = Encoding.ASCII.GetBytes("rejected");
                    }
                    clientSocket.Send(dataToClient);
                }
                else
                {
                    var dataToClient = Encoding.ASCII.GetBytes("expired");
                    clientSocket.Send(dataToClient);
                }
            }
            //End Logic
            clientSocket.BeginReceive(_buffer, 0, _bufferSize, SocketFlags.None, AsyncRecieveCallback, clientSocket);
            try
            {
                var expiredAuctions = auctions.Count(x => !x.active);
                if (expiredAuctions > 0)
                {
                    //foreach(var _item in expiredAuctions)
                    //{
                    //    auctions.Remove(_item);
                    //}
                    for (int i = expiredAuctions; i > 0; i--)
                    {
                        var rnd = new Random();
                        var auction = new AuctionItems(rnd.Next(100));
                        auctions.Add(auction);
                    }
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("No expired Auctions");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException.Message);
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "Server";
            ServerInit();
            Console.ReadLine();
        }
    }

    public class AuctionItems
    {
        public int _currentBid;
        public int currentBid
        {
            get
            {
                return _currentBid;
            }
            set
            {
                if (value < 0)
                    _currentBid = -1;
                else
                {
                    if (value < _maxValue && _active)
                        _currentBid = value;
                    else
                        _active = false;

                }
            }
        }
        public int maxValue { get { return _maxValue; } }
        public Guid pid { get { return _pid; } }
        public bool active { get { return _active; } }

        private Guid _pid { get; set; }
        private int _maxValue;
        private bool _active;


        public AuctionItems(int maxValue)
        {
            this._maxValue = maxValue;
            this._pid = Guid.NewGuid();
            this._currentBid = 0;
            this._active = true;
        }

        public AuctionItems(string pid, int maxValue, int currentBid, bool active)
        {
            this._maxValue = maxValue;
            this._pid = new Guid(pid);
            this._currentBid = currentBid;
            this._active = active;
        }
        public override string ToString()
        {
            return _pid + ";" + maxValue + ";" + currentBid + ";" + active;
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
