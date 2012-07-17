using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BattleShipsServer
{
    class Server
    {
        static void Main(string[] args)
        {
            TCPServer gameServer = new TCPServer(IPAddress.Any, 7168);
        }
    }
}
