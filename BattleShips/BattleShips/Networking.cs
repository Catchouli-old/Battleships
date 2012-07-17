using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;

namespace BattleShips
{
    public class TCPClient
    {
        BattleShips _game;
        Socket tcpSocket;
        byte[] buffer = new byte[1024];
        IPEndPoint serverEndpoint;
        public bool running = true;

        public TCPClient(BattleShips game, IPAddress serverAddress, int port)
        {
            _game = game;
            Console.WriteLine("Creating TCP client");
            this.serverEndpoint = new IPEndPoint(serverAddress, port);
            Thread connectionThread = new Thread(new ThreadStart(AttemptConnection));
            connectionThread.Start();
        }

        private void AttemptConnection()
        {
            Console.WriteLine("Attempting to connect...");

            tcpSocket = new Socket(AddressFamily. InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.BeginConnect(serverEndpoint, new AsyncCallback(ConnectCallback), tcpSocket);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            if (!running)
                return;
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
                Console.WriteLine("Connected to server.");
                SendData(new byte[1] { 2 });
                new Thread(CheckConnected).Start();
                //byte[] buffer = new byte[1 + 100];
                //buffer[0] = 17;
                //int[] points = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                //Array.Copy(BitConverter.GetBytes(points.Length), 0, buffer, 1, 4);
                //for (int i = 0; i < points.Length; i++)
                //{
                //    Array.Copy(BitConverter.GetBytes(points[i]), 0, buffer, 5 + i * 4, 4);
                //}
                //SendData(buffer);
                new Thread(SyncPlayers).Start();
                Receive(tcpSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                _game._connectionClient.running = false;
                _game.Exit();
            }
        }

        private void CheckConnected()
        {
            //while (true)
            //{
            //    Thread.Sleep(30000);
            //    if (!running)
            //        return;
            //    try
            //    {
            //        SendData(new byte[] { 1 });
            //    }
            //    catch (Exception)
            //    {
            //        this.running = false;
            //        _game.Exit();
            //    }
            //}
        }

        private void SyncPlayers()
        {
            if (!running)
                return;
            Thread.Sleep(500);
            if (_game._myForm.PlayerCount <= 0 || _game._myForm._myPlayerId == -1)
            {
                SendData(new byte[] { 2 });
                new Thread(SyncPlayers).Start();
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                // Begin receiving the data from the remote device.
                client.BeginReceive(buffer, 0, this.buffer.Length, 0,
                    new AsyncCallback(ReceiveCallback), this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!running)
                return;
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                TCPClient state = (TCPClient)ar.AsyncState;
                Socket client = state.tcpSocket;
                // Read data from the remote device.
                int bytesRead = 0;
                try
                {
                    bytesRead = client.EndReceive(ar);
                }
                catch (Exception)
                {
                    _game._connectionClient = new TCPClient(_game, serverEndpoint.Address, serverEndpoint.Port);
                }
                if (bytesRead > 0)
                {
                    byte packetId = buffer[0];
                    byte[] temp;

                    int playerId;

                    switch (packetId)
                    {
                        case 0:
                            SendData(new byte[] { 1 });
                            Console.WriteLine("Responding to ping");
                            break;
                        case 3:
                            // Receive list of players
                            _game._myForm.ClearPlayers();
                            for (int i = 1; i + 8 < bytesRead; )
                            {
                                Console.WriteLine("Receiving name");
                                playerId = BitConverter.ToInt32(buffer, i);
                                int _playerNameLength = BitConverter.ToInt32(buffer, i + 4);
                                string _playerName = ASCIIEncoding.ASCII.GetString(buffer, i + 8, _playerNameLength);
                                _game._myForm.AddPlayer(playerId, _playerName);
                                i += 8 + _playerNameLength;
                            }
                            break;
                        case 7:
                            // Check message length
                            temp = new byte[4];
                            Array.Copy(buffer, 1, temp, 0, 4);
                            int messageLength = BitConverter.ToInt32(temp, 0);
                            // Parse message
                            temp = new byte[messageLength];
                            Array.Copy(buffer, 5, temp, 0, messageLength);
                            string messageString = ASCIIEncoding.ASCII.GetString(temp);
                            Console.WriteLine("Receiving new chat message  of length {0}", messageLength);
                            // Output message
                            if (_game._myForm != null)
                                _game._myForm.AddChatMessage(messageString);
                            if (messageString.Equals("Both players are ready, commence battle!"))
                            {
                                _game._myForm.ToggleButtons(false);
                                _game._gameState = GameState.MAKETURN;
                            }
                            break;
                        case 8:
                            playerId = BitConverter.ToInt32(buffer, 1);
                            int playerNameLength = BitConverter.ToInt32(buffer, 5);
                            string playerName = ASCIIEncoding.ASCII.GetString(buffer, 9, playerNameLength);
                            Console.WriteLine("Adding player {0}, with ID {1}", playerName, playerId);
                            _game._myForm.AddPlayer(playerId, playerName);
                            break;
                        case 10:
                            playerId = BitConverter.ToInt32(buffer, 1);
                            _game._myForm.RemovePlayer(playerId);
                            break;
                        case 15:
                            // Parse our player ID
                            playerId = BitConverter.ToInt32(buffer, 1);
                            _game._myForm._myPlayerId = playerId;
                            _game._playerName = ASCIIEncoding.ASCII.GetString(buffer, 9, BitConverter.ToInt32(buffer, 5));
                            _game._myForm.SetTitle("Connected to " + serverEndpoint.ToString() + " as " + _game._playerName);
                            break;
                        case 16:
                            /*
                             * Packet ID: 16
                             * InitiateGame
                             */
                            Console.WriteLine("Disabling interface, new game starting.");
                            _game._myForm.DisableInterface();
                            _game._myForm.ShowButtons(true);
                            _game._gameState = GameState.SETBOATS;
                            break;
                        case 19:
                            _game.ResetGame();
                            break;
                        case 20:
                            if (bytesRead < 5)
                                _game._myForm.ToggleButtons();
                            else
                                _game._myForm.ToggleButtons((BitConverter.ToInt32(buffer, 1) == 0 ? false : true));
                            break;
                        case 21:
                            if (bytesRead < 5)
                                _game._myForm.SetButtons(true);
                            else
                                _game._myForm.SetButtons((BitConverter.ToInt32(buffer, 1) == 0 ? false : true));
                            break;
                        case 23:
                            List<Point> targets = new List<Point>();
                            int numberOfNumbers = BitConverter.ToInt32(buffer, 1);
                            for (int i = 0; i < numberOfNumbers; i++)
                            {
                                Point receivedPoint = new Point(BitConverter.ToInt32(buffer, (8 * i) + 5), BitConverter.ToInt32(buffer, (8 * i) + 9));
                                Console.WriteLine("Received point ({0}, {1}) - HIT!!!", receivedPoint.X, receivedPoint.Y);
                                targets.Add(receivedPoint);
                            }
                            _game._bombedTiles.AddRange(_game._targetedTiles);
                            _game._targetedTiles.Clear();
                            _game._hitTiles.AddRange(targets);
                            _game._myForm.ToggleButtons(false);
                            _game._myForm.AddChatMessage("You hit " + targets.Count + " enemy ships, " + (17 - _game._hitTiles.Count) + " remaining.");
                            SendData(new byte[] { 25 });
                            break;
                        case 24:
                            int outcome = BitConverter.ToInt32(buffer, 1);
                            if (outcome == 0)
                            {
                                // draw
                                _game._myForm.AddChatMessage("You tied, still a loser.");
                            }
                            else if (outcome == 1)
                            {
                                // Win
                                _game._myForm.AddChatMessage("You won, loser.");
                            }
                            else
                            {
                                // Lose
                                _game._myForm.AddChatMessage("You lost, loser.");
                            }
                            _game.ResetGame();
                            break;
                        case 26:
                            List<Point> newHits = new List<Point>();
                            int numberOfNumbers2 = BitConverter.ToInt32(buffer, 1);
                            for (int i = 0; i < numberOfNumbers2; i++)
                            {
                                Point newPoint = new Point(BitConverter.ToInt32(buffer, (8 * i) + 5), BitConverter.ToInt32(buffer, (8 * i) + 9));
                                Console.WriteLine("Your ({0}, {1}) was hit.", newPoint.X.ToString(), newPoint.Y.ToString());
                                newHits.Add(newPoint);
                            }
                            _game._myHitTiles.AddRange(newHits);
                            break;
                        default:
                            Console.WriteLine("Unknown packet ID: {0}", packetId);
                            break;
                    }
                    //  Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, state.buffer.Length, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendData(byte[] buffer)
        {
            if (tcpSocket.Connected)
            {
                tcpSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), tcpSocket);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendChallenge(int playerId)
        {
            byte[] buffer = new byte[5];
            buffer[0] = 12;
            Array.Copy(BitConverter.GetBytes(playerId), 0, buffer, 1, 4);
            SendData(buffer);
        }

        public void CancelChallenge(int playerId)
        {
            byte[] buffer = new byte[5];
            buffer[0] = 13;
            Array.Copy(BitConverter.GetBytes(playerId), 0, buffer, 1, 4);
            SendData(buffer);
        }

        public void Forfeit()
        {
            SendData(new byte[1] { 18 });
        }

        public void SendTargets()
        {
            if (_game._targetedTiles.Count == 5)
            {
                byte[] buffer = new byte[41];
                buffer[0] = 22;

                for (int i = 0; i < 5; i++)
                {
                    Array.Copy(BitConverter.GetBytes(_game._targetedTiles[i].X), 0, buffer, (8 * i) + 1, 4);
                    Array.Copy(BitConverter.GetBytes(_game._targetedTiles[i].Y), 0, buffer, (8 * i) + 5, 4);
                }

                SendData(buffer);
            }
        }
    }

    enum NetworkMode
    {
        SERVER,
        CLIENT
    }

    public enum ConnectionError
    {
        LISTENING_SOCKET_IN_USE,
        CONNECTION_EXITED,
        REJECTED_BY_HOST,
        ERROR_UNKNOWN
    }
}
