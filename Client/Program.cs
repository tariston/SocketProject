using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
            //set hostname
            Guid g = Guid.NewGuid();
            Console.WriteLine(g);
            var hostname = g.ToString();
            hostname = "hostname " + hostname;
            Console.WriteLine("Sending " + hostname);
            sendData(hostname);
            recieveData();
            while(true)
            {
                //get auctions
                var rnd = new Random();
                string command = "list";
                Console.WriteLine("Sending Command: " + command);
                sendData(command);
                var receivedData = recieveData();
                var auctions = receivedData.Split(',');
                var pick = rnd.Next(auctions.Count());
                //            return _pid + ";" + maxValue + ";" + currentBid + ";" + active;
                var raw = auctions[pick].Split(';');
                var bidding = Convert.ToBoolean(raw[3]);
                var auctionId = raw[0];
                int mybid = Convert.ToInt32(raw[2]) + 1;
                int maxBid = Convert.ToInt32(raw[1]);

                while(bidding)
                {
                    //bid
                    command = "bid " + auctionId + " " + mybid;
                    Console.WriteLine(command);
                    sendData(command);
                    receivedData = recieveData();
                    if (receivedData.ToLower().StartsWith("expired"))
                    {
                        Console.WriteLine("Expired");
                        bidding = false; 
                    }
                        
                    else if (receivedData.ToLower().StartsWith("accepted"))
                    {
                        Console.WriteLine("Bid Accepted");
                        mybid = mybid+1;
                    }
                    else if (receivedData.ToLower().StartsWith("rejected"))
                    {
                        command = "price " + auctionId;
                        Console.WriteLine(command); 
                        sendData(command);
                        if (receivedData.StartsWith("expired"))
                            bidding = false;
                        receivedData = recieveData();
                        mybid = Convert.ToInt32(receivedData) + 1;
                    }

                    Thread.Sleep(rnd.Next(1000));
                    //get currentBid
                }
                Thread.Sleep(rnd.Next(1000));
                
                //Thread.Sleep(5000);
                //select auction
            }
        }

        private static string recieveData()
        {
            byte[] data = new byte[1024];
            int length = _socket.Receive(data, SocketFlags.None);
            byte[] trimmed = new byte[length];
            Array.Copy(data, trimmed, length);
            var incomingData =Encoding.ASCII.GetString(trimmed); 
            Console.WriteLine(incomingData);
            return incomingData;
        }

        private static void sendData(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            _socket.Send(data, 0, data.Length, SocketFlags.None);
        }

        static void Main(string[] args)
        {
            Console.Title = "Client";
            connect();
            Console.ReadLine();
        }
    }
}
