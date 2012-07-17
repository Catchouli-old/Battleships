using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BattleShipsServer
{
    public class Client
    {
        int _id;
        string _name;
        public List<Client> Challenges;
        Game _currentGame;
        public Socket _client;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public bool _gotPing = true;
        public List<Point> targets;
        public List<Point> lastHit;

        public Client(int id, string name, Socket tcpSocket)
        {
            ID = id;
            Name = name;
            _client = tcpSocket;
            Challenges = new List<Client>();
            targets = new List<Point>();
            lastHit = new List<Point>();
        }

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                Name = "Player " + _id;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public Socket TcpSocket
        {
            get
            {
                return _client;
            }
            set
            {

            }
        }

        public Game CurrentGame
        {
            get
            {
                return _currentGame;
            }
            set
            {
                _currentGame = value;
            }
        }
    }
}
