using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BattleShipsServer
{
    class ClientList
    {
        int _nextClientId = 0;
        public List<Client> _tcpClients;
        TCPServer _server;

        public ClientList(TCPServer server)
        {
            _server = server;
            _tcpClients = new List<Client>();
        }

        public int Add(Client client)
        {
            Console.WriteLine("Adding client with ID " + (_nextClientId++).ToString());
            client.ID = _nextClientId - 1;
            _tcpClients.Add(client);
            return _tcpClients.Last().ID;
        }

        public int Add(Socket tcpClient)
        {

            return Add(new Client(-1, "Unnamed", tcpClient));
        }

        public void Remove(Client client)
        {
            _tcpClients.Remove(client);
            _server.RemovePlayerAll(client);
            Console.WriteLine("Removing client " + client.ID);
        }

        public void Remove(int clientId)
        {
            Client client = GetClient(clientId);
            if (client != null)
            {
                Remove(GetClient(clientId));
            }
        }

        public Client GetClient(int id)
        {
            foreach (Client client in _tcpClients)
            {
                if (client.ID == id)
                    return client;
            }

            return null;
        }

        public Socket GetTcpClient(int id)
        {
            foreach (Client client in _tcpClients)
            {
                if (client.ID == id)
                    return client.TcpSocket;
            }

            return null;
        }

        public Socket Last()
        {
            return _tcpClients.Last().TcpSocket;
        }
    }
}
