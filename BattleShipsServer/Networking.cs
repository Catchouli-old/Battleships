using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BattleShipsServer
{
    public class TCPServer
    {
        private TcpListener tcpListener;
        private ClientList tcpClients;
        bool running = true;

        public TCPServer(IPAddress listenAddress, int port)
        {
            Console.WriteLine("TCP Server started");
            this.tcpListener = new TcpListener(listenAddress, port);
            tcpClients = new ClientList(this);
            ListenForClients();
        }

        private void ListenForClients()
        {
            Console.WriteLine("Listening socket opened");

            try
            {
                this.tcpListener.Start();
            }
            catch (SocketException)
            {

            }

            while (running)
            {
                Client newClient = new Client(-1, "name", tcpListener.AcceptSocket());

                tcpClients.Add(newClient);

                newClient.TcpSocket.BeginReceive(newClient.buffer, 0, newClient.buffer.Length, 0, new AsyncCallback(ReadData), newClient);
                new Thread(new ParameterizedThreadStart(CheckPing)).Start(newClient);

                for (int i = 0; i < tcpClients._tcpClients.Count; i++)
                {
                    if (tcpClients._tcpClients[i] != null && tcpClients._tcpClients[i] != newClient)
                        AddPlayer(newClient, tcpClients._tcpClients[i]);
                }

                SendData(newClient.ID, Encoding.ASCII.GetBytes("Welcome to battleships"));

                //if (_clientThread == null)
                //{
                //    _clientThread = new Thread(ClientLoop);
                //    _clientThread.Start();
                //}
                //SendData(lastClientId, new ASCIIEncoding().GetBytes("Connected to server\n"));
            }
        }

        private void ReadData(IAsyncResult ar)
        {
            Client client = (Client)ar.AsyncState;
            Socket handler = client.TcpSocket;
            try
            {
                int read = handler.EndReceive(ar);

                if (read > 0)
                {
                    Console.WriteLine("Receiving packet {0}, arguments: {1}", client.buffer[0], client.buffer);

                    byte packetId = client.buffer[0];
                    int playerId;
                    Client otherClient;

                    switch (packetId)
                    {
                        case 1:
                            client._gotPing = true;
                            Console.WriteLine("Got pong from client {0}", client.ID.ToString());
                            break;
                        case 2:
                            /* Packet ID 2
                             * RequestPlayerList
                             */
                            SendPlayerId(client);
                            SyncPlayers(client);
                            break;
                        case 6:
                            /* Packet ID 6
                             * Format: 6 XX XX YY YY YY YY YY YY .. etc
                             * int16 XX = number of characters following
                             * string YY... = message (string)
                             */
                            // Check message length
                            byte[] temp = new byte[4];
                            Array.Copy(client.buffer, 1, temp, 0, 4);
                            int messageLength = BitConverter.ToInt32(temp, 0);
                            temp = new byte[messageLength];
                            // Parse message
                            Array.Copy(client.buffer, 5, temp, 0, messageLength);
                            string messageString = ASCIIEncoding.ASCII.GetString(temp);
                            // Send it to all clients
                            string name = "Player " + client.ID;
                            temp = new byte[9 + name.Length + messageString.Length];
                            temp[0] = 7;
                            // Encode Message
                            messageString = name + ": " + messageString;
                            SendChatMessage(messageString);
                            break;
                        case 12:
                            playerId = BitConverter.ToInt32(client.buffer, 1);
                            otherClient = tcpClients.GetClient(playerId);
                            if (client.Challenges.Contains(otherClient))
                            {
                                client.Challenges.Remove(otherClient);
                                InitiateGame(otherClient, client);
                                SendChatMessage(otherClient, client.Name + " has accepted your challenge!\r\nSet up your defences by dragging your mouse on the left grid. Press set defences when finished.");
                                SendChatMessage(client, "You have accepted " + otherClient.Name + "'s challenge!\r\nSet up your defences by dragging your mouse on the left grid. Press set defences when finished.");
                            }
                            else
                            {
                                otherClient.Challenges.Add(client);
                                SendChatMessage(otherClient, "You have received a challenge from " + client.Name);
                            }
                            break;
                        case 13:
                            playerId = BitConverter.ToInt32(client.buffer, 1);
                            Console.WriteLine("Received challenge cancelation from {0} to {1}", client.ID.ToString(), playerId.ToString());
                            otherClient = tcpClients.GetClient(playerId);
                            otherClient.Challenges.Remove(client);
                            SendChatMessage(otherClient, "The challenge from wussy mc'" + client.Name + " has been cancelled.");
                            break;
                        case 17:
                            /*
                             * Packet ID: 17
                             * SendBoats
                             * Receive boats from client
                             */
                            if (client.CurrentGame != null)
                            {
                                if (read == 137)
                                {
                                    otherClient = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwo : client.CurrentGame._playerOne);
                                    List<Point> boats = new List<Point>();
                                    for (int i = 0; i < 17; i++)
                                    {
                                        boats.Add(new Point(BitConverter.ToInt32(client.buffer, 1 + (i * 8)), BitConverter.ToInt32(client.buffer, 5 + (i * 8))));
                                        Console.WriteLine("Read boat ({0}, {1})", boats.Last().X.ToString(), boats.Last().Y.ToString());
                                    }
                                    if (boats.Count < 17)
                                    {
                                        SendChatMessage(client, "You must set 17 squares to be occupied by boats.");
                                    }
                                    else
                                    {
                                        if (client.CurrentGame.AddBoat(client, boats))
                                        {
                                            SendData(client, new byte[] { 21, 0, 0, 0, 0 });
                                            if ((client == client.CurrentGame._playerOne ? otherClient.CurrentGame._playerTwoState : otherClient.CurrentGame._playerOneState) == PlayerState.DONE)
                                            {
                                                // Both players are ready!
                                                SendChatMessage(client, "Both players are ready, commence battle!");
                                                SendChatMessage(otherClient, "Both players are ready, commence battle!");
                                            }
                                            else
                                            {
                                                SendChatMessage(client, "Waiting for other player to finish setting defences.");
                                                SendChatMessage(otherClient, "Other player is finished setting defences");
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case 18:
                            if (client.CurrentGame != null)
                            {
                                otherClient = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwo : client.CurrentGame._playerOne);
                                client.CurrentGame = null;
                                otherClient.CurrentGame = null;
                                temp = new byte[1] { 19 };
                                SendData(client, temp);
                                SendData(otherClient, temp);
                                SendChatMessage(client, "You have forfeited your match to " + otherClient.Name);
                                SendChatMessage(otherClient, client.Name + " waves a white flag. You are victorious.");
                            }
                            break;
                        case 22:
                            if (client.CurrentGame != null)
                            {
                                if (read == 41)
                                {
                                    otherClient = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwo : client.CurrentGame._playerOne);
                                    // Read targets from packet
                                    client.targets.Clear();
                                    List<Point> newList = new List<Point>();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        Point receivedPoint = new Point(BitConverter.ToInt32(client.buffer, (8 * i) + 1), BitConverter.ToInt32(client.buffer, (8 * i) + 5));
                                        Console.WriteLine("Received point ({0}, {1})", receivedPoint.X, receivedPoint.Y);
                                        newList.Add(new Point(BitConverter.ToInt32(client.buffer, (8 * i) + 1), BitConverter.ToInt32(client.buffer, (8 * i) + 5)));
                                    }
                                    client.targets.AddRange(newList);
                                    otherClient.lastHit.AddRange(newList);

                                    if (otherClient.targets.Count != 0)
                                    {
                                        SendHit(client);
                                        SendHit(otherClient);
                                        RemoveHit(client);
                                        RemoveHit(otherClient);
                                        if (client.CurrentGame._playerOneAllSquares.Count == 0 && client.CurrentGame._playerTwoAllSquares.Count == 0)
                                        {
                                            // It's a draw
                                            new Thread(new ParameterizedThreadStart(SendResult)).Start(client.CurrentGame);
                                            Console.WriteLine("Draw");
                                        }
                                        else if (client.CurrentGame._playerOneAllSquares.Count == 0 || client.CurrentGame._playerTwoAllSquares.Count == 0)
                                        {
                                            // One player won!
                                            Client winner = (client.CurrentGame._playerTwoAllSquares.Count == 0 ? client.CurrentGame._playerOne : client.CurrentGame._playerTwo);
                                            Client loser = (client.CurrentGame._playerTwoAllSquares.Count == 0 ? client.CurrentGame._playerTwo : client.CurrentGame._playerOne);
                                            client.CurrentGame.winner = winner;
                                            new Thread(new ParameterizedThreadStart(SendResult)).Start(client.CurrentGame);

                                            Console.WriteLine(winner.Name + " wins!");
                                        }
                                        client.targets.Clear();
                                        otherClient.targets.Clear();
                                    }
                                    else
                                    {
                                        byte[] buffer = new byte[5] { 21, 0, 0, 0, 0 };
                                        SendData(client, buffer);
                                    }
                                }
                            }
                            break;
                        case 25:
                            if (client.CurrentGame != null)
                            {
                                temp = new byte[5 + (client.lastHit.Count * 8)];
                                temp[0] = 26;

                                Array.Copy(BitConverter.GetBytes(client.lastHit.Count), 0, temp, 1, 4);
                                for (int i = 0; i < client.lastHit.Count; i++)
                                {
                                    Array.Copy(BitConverter.GetBytes(client.lastHit[i].X), 0, temp, 5 + (i * 8), 4);
                                    Array.Copy(BitConverter.GetBytes(client.lastHit[i].Y), 0, temp, 9 + (i * 8), 4);
                                }
                                SendData(client, temp);
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown packet {0}", packetId);
                            break;
                    }
                }
                client.TcpSocket.BeginReceive(client.buffer, 0, client.buffer.Length, 0, new AsyncCallback(ReadData), client);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Client {0} disconnected.", client.ID);
                tcpClients.Remove(client);
            }
        }

        public void SendResult(object arguments)
        {
            Thread.Sleep(3000);
            Game game = (Game)arguments;
            if (game != null)
            {
                if (game.winner == null)
                {
                    // It was a tie
                    SendData(game._playerOne, new byte[] { 24, 0, 0, 0, 0 });
                    SendData(game._playerTwo, new byte[] { 24, 0, 0, 0, 0 });
                }
                else if (game.winner == game._playerOne)
                {
                    // Player one won
                    byte[] data = new byte[] { 24, 0, 0, 0, 0 };
                    Array.Copy(BitConverter.GetBytes(1), 0, data, 1, 4);
                    SendData(game._playerOne, data);
                    Array.Copy(BitConverter.GetBytes(2), 0, data, 1, 4);
                    SendData(game._playerTwo, data);
                }
                else if (game.winner == game._playerTwo)
                {
                    // Player two won
                    byte[] data = new byte[] { 24, 0, 0, 0, 0 };
                    Array.Copy(BitConverter.GetBytes(2), 0, data, 1, 4);
                    SendData(game._playerOne, data);
                    Array.Copy(BitConverter.GetBytes(1), 0, data, 1, 4);
                    SendData(game._playerTwo, data);
                }

                // Clean up
                game._playerOne.CurrentGame = null;
                game._playerTwo.CurrentGame = null;
            }
        }

        /*
         * Packet ID: 3
         * SyncPlayerList
         * Sends all client information to one client
         */
        public void SyncPlayers(Client client)
        {
            Console.WriteLine("Sending names");
            int packetLength = 1;
            foreach (Client otherClient in tcpClients._tcpClients)
            {
                packetLength += 8;
                packetLength += otherClient.Name.Length;
            }

            byte[] buffer = new byte[packetLength];
            buffer[0] = 3;
            int packetCounter = 1;
            for (int i = 0; i < tcpClients._tcpClients.Count; i++)
            {
                Array.Copy(BitConverter.GetBytes(tcpClients._tcpClients[i].ID), 0, buffer, packetCounter, 4);
                Array.Copy(BitConverter.GetBytes(tcpClients._tcpClients[i].Name.Length), 0, buffer, packetCounter + 4, 4);
                Array.Copy(ASCIIEncoding.ASCII.GetBytes(tcpClients._tcpClients[i].Name), 0, buffer, packetCounter + 8, tcpClients._tcpClients[i].Name.Length);
                packetCounter += 8 + tcpClients._tcpClients[i].Name.Length;
            }
            Console.WriteLine("Buffer length: " + buffer.Length);

            SendData(client, buffer);
        }

        public void CheckPing(Object clientObject)
        {
            Client client = (Client)clientObject;
            while (true)
            {
                Console.WriteLine("Checking ping reply");
                if (client._gotPing)
                {
                    client._gotPing = false;
                    SendData(client, new byte[1] { 0 });
                }
                else
                {
                    Console.WriteLine("Client {0} pinged out", client.Name);
                    SendChatMessage(client.Name + " has quit (Ping timeout: 180 seconds)");
                    try
                    {
                        client.TcpSocket.Disconnect(true);
                    }
                    catch (SocketException)
                    {

                    }
                    tcpClients._tcpClients.Remove(client);
                    tcpClients.Remove(client);
                    break;
                }
                Thread.Sleep(180000);
            }
        }

        /*
         * Packet ID: 7
         * Sends a chat message to one or more clients
         */
        public void SendChatMessage(string message)
        {
            byte[] temp = new byte[5 + message.Length];
            temp[0] = 7;
            Array.Copy(BitConverter.GetBytes(message.Length), 0, temp, 1, 4);
            Array.Copy(ASCIIEncoding.ASCII.GetBytes(message), 0, temp, 5, message.Length);
            SendAll(temp);
        }
  
        public void SendChatMessage(Client client, string message)
        {
            byte[] temp = new byte[5 + message.Length];
            temp[0] = 7;
            Array.Copy(BitConverter.GetBytes(message.Length), 0, temp, 1, 4);
            Array.Copy(ASCIIEncoding.ASCII.GetBytes(message), 0, temp, 5, message.Length);
            SendData(client, temp);
        }

        /*
         * Packet ID: 8
         * Sends new client information to all connected clients
         */
        public void AddPlayer(Client client)
        {
            string name = client.Name;
            byte[] buffer = new byte[9 + name.Length];
            buffer[0] = 8;
            Array.Copy(BitConverter.GetBytes(client.ID), 0, buffer, 1, 4);
            Array.Copy(BitConverter.GetBytes(name.Length), 0, buffer, 5, 4);
            Array.Copy(ASCIIEncoding.ASCII.GetBytes(name), 0, buffer, 9, name.Length);
            SendAll(buffer);
        }

        public void AddPlayer(Client addClient, Client toClient)
        {
            Console.WriteLine("Sending player {0} to player {1}", addClient.ID, toClient.ID);
            string name = addClient.Name;
            byte[] buffer = new byte[9 + name.Length];
            buffer[0] = 8;
            Array.Copy(BitConverter.GetBytes(addClient.ID), 0, buffer, 1, 4);
            Array.Copy(BitConverter.GetBytes(name.Length), 0, buffer, 5, 4);
            Array.Copy(ASCIIEncoding.ASCII.GetBytes(name), 0, buffer, 9, name.Length);
            SendData(toClient, buffer);
        }
        
        /*
         * Packet ID: 10
         * Sends new client removed information to a client
         */
        public void RemovePlayerAll(Client client)
        {
            Console.WriteLine("Syncing removed player to all players");
            byte[] buffer = new byte[5];
            buffer[0] = 10;
            Array.Copy(BitConverter.GetBytes(client.ID), 0, buffer, 1, 4);
            SendAll(buffer);
        }
        
        /*
         * Packet ID: 15
         * SendPlayerId
         * Sends player their player ID
         */
        public void SendPlayerId(Client client)
        {
            byte[] buffer = new byte[9 + client.Name.Length];
            buffer[0] = 15;
            Array.Copy(BitConverter.GetBytes(client.ID), 0, buffer, 1, 4);
            Array.Copy(BitConverter.GetBytes(client.Name.Length), 0, buffer, 5, 4);
            Array.Copy(ASCIIEncoding.ASCII.GetBytes(client.Name), 0, buffer, 9, client.Name.Length);
            SendData(client, buffer);
        }

        /*
         * Packet ID: 16
         * InitiateGame
         */
        public void InitiateGame(Client playerOne, Client playerTwo)
        {
            if (playerOne.TcpSocket.Connected && playerTwo.TcpSocket.Connected)
            {
                byte[] buffer = new byte[5];
                buffer[0] = 16;
                // Pack player two's ID and send it to player 1 with an InitiateGame packet
                Array.Copy(BitConverter.GetBytes(playerTwo.ID), 0, buffer, 1, 4);
                SendData(playerOne, buffer);
                // Pack player one's ID and send it to player 2
                Array.Copy(BitConverter.GetBytes(playerOne.ID), 0, buffer, 1, 4);
                SendData(playerTwo, buffer);

                Game newGame = new Game(playerOne, playerTwo);
                playerOne.CurrentGame = newGame;
                playerTwo.CurrentGame = newGame;
            }
        }
        
        /*
         * Packet ID: 23
         * SendHit
         */
        public void RemoveHit(Client client)
        {
            Client otherClient = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwo : client.CurrentGame._playerOne);
            List<Point> otherClientPoints = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwoAllSquares : client.CurrentGame._playerOneAllSquares);
            // Build thingy to send back
            List<Point> hits = new List<Point>();
            foreach (Point point in client.targets)
            {
                for (int i = 0; i < otherClientPoints.Count; i++)
                {
                    if (point.X == otherClientPoints[i].X && point.Y == otherClientPoints[i].Y)
                    {
                        otherClientPoints.Remove(otherClientPoints[i]);
                        i--;
                        continue;
                    }
                }
            }
        }

        public void SendHit(Client client)
        {
            Client otherClient = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwo : client.CurrentGame._playerOne);
            List<Point> otherClientPoints = (client == client.CurrentGame._playerOne ? client.CurrentGame._playerTwoAllSquares : client.CurrentGame._playerOneAllSquares);
            // Build thingy to send back
            List<Point> hits = new List<Point>();
            foreach (Point point in client.targets)
            {
                foreach (Point otherPoint in otherClientPoints)
                {
                    if (point.X == otherPoint.X && point.Y == otherPoint.Y)
                    {
                        hits.Add(point);
                        //otherClientPoints.Remove(otherPoint);
                    }
                }
            }

            byte[] temp = new byte[5 + (8 * hits.Count)];
            temp[0] = 23;
            Array.Copy(BitConverter.GetBytes(hits.Count), 0, temp, 1, 4);

            for (int i = 0; i < hits.Count; i++)
            {
                Array.Copy(BitConverter.GetBytes(hits[i].X), 0, temp, (8 * i) + 5, 4);
                Array.Copy(BitConverter.GetBytes(hits[i].Y), 0, temp, (8 * i) + 9, 4);
            }

            SendData(client, temp);

            // Check if the player has won
        }

        private void SendData(int clientId, byte[] buffer)
        {
            Client client = tcpClients.GetClient(clientId);
            if (client != null)
            {
                if (buffer.Length <= 1024)
                {
                    try
                    {
                        client.TcpSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
                    }
                    catch (SocketException)
                    {
                        tcpClients._tcpClients.Remove(client);
                    }
                }
            }
        }

        private void SendData(Client client, byte[] buffer)
        {
            if (buffer.Length <= 1024)
            {
                try
                {
                    client.TcpSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
                }
                catch (SocketException)
                {
                    tcpClients._tcpClients.Remove(client);
                }
            }
        }

        private void SendAll(byte[] buffer)
        {
            try
            {
                if (buffer.Length <= 1024)
                {
                    foreach (Client otherClient in tcpClients._tcpClients)
                    {
                        SendData(otherClient, buffer);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Client client = (Client)ar.AsyncState;
                Socket handle = (Socket)client.TcpSocket;

                int bytesSent = handle.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client {1}", bytesSent, client.ID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        //private void ClientLoop()
        //{
        //    while (running)
        //    {
        //        for (int i = 0; i < tcpClients._tcpClients.Count; i++)
        //        {
        //            if (tcpClients._tcpClients[i]._gotPing)
        //            {
        //                tcpClients._tcpClients[i]._gotPing = false;
        //                SendData(tcpClients._tcpClients[i].ID, new byte[] { 0 });
        //            }
        //            else
        //            {
        //                // User has had a ping timeout
        //                SendData(tcpClients._tcpClients[i].ID, new ASCIIEncoding().GetBytes("You have ping timeouted"));
        //                tcpClients._tcpClients[i].TcpClient.Close();
        //                tcpClients.Remove(tcpClients._tcpClients[i]);
        //                i--;
        //                continue;
        //            }
        //        }
        //        System.Threading.Thread.Sleep(3000);
        //    }
        //}

        //public void SendData(int id, byte[] buffer)
        //{
        //    Client client = tcpClients.GetClient(id);
        //    if (client != null)
        //    {
        //        if (client.TcpClient.Connected)
        //        {
        //            try
        //            {
        //                NetworkStream clientStream = client.TcpClient.GetStream();
        //                ASCIIEncoding encoder = new ASCIIEncoding();

        //                clientStream.Write(buffer, 0, buffer.Length);
        //                ((Socket)client.TcpClient).BeginSend(buffer, 0, buffer.Length, SocketFlags.None);
        //                clientStream.Flush();
        //            }
        //            catch (Exception)
        //            {
        //                tcpClients.Remove(client);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        tcpClients.Remove(client);
        //    }
        //}

        //private void CompleteSend(IAsyncResult ar)
        //{
        //    try
        //    {
        //        Socket client = (Socket)ar.AsyncState;

        //    }
        //    catch (Exception e)
        //    {

        //    }
        //}

        //public void Close()
        //{
        //    foreach (Client tcpClient in tcpClients._tcpClients)
        //    {
        //        tcpClient._client.Close();
        //    }
        //    running = false;
        //}
    }
}
