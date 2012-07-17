using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BattleShipsServer
{
    public class Game
    {
        GameState _gameState;

        public Client _playerOne;
        public PlayerState _playerOneState;
        public List<Point> _playerOneAllSquares;

        public Client _playerTwo;
        public PlayerState _playerTwoState;
        public List<Point> _playerTwoAllSquares;

        public Client winner;

        public Game(Client playerOne, Client playerTwo)
        {
            Console.WriteLine("New game created between {0} and {1}", playerOne.Name, playerTwo.Name);
            _gameState = GameState.SETBOATS;

            _playerOne = playerOne;
            _playerOneState = PlayerState.WAITING;
            _playerOneAllSquares = new List<Point>();

            _playerTwo = playerTwo;
            _playerTwoState = PlayerState.WAITING;
            _playerTwoAllSquares = new List<Point>();
        }

        public bool AddBoat(Client client, List<Point> boat)
        {
            Console.WriteLine("Adding boat for {0}", client.Name);
            if (_gameState == GameState.SETBOATS)
            {
                if (client == _playerOne && _playerOneState == PlayerState.WAITING)
                {
                    _playerOneAllSquares.AddRange(boat);
                    if (_playerOneAllSquares.Count > 17)
                        _playerOneAllSquares.Clear();
                    if (_playerOneAllSquares.Count >= 17)
                    {
                        _playerOneState = PlayerState.DONE;
                        return true;
                    }
                }
                else if (client == _playerTwo && _playerTwoState == PlayerState.WAITING)
                {
                    _playerTwoAllSquares.AddRange(boat);
                    if (_playerTwoAllSquares.Count > 17)
                        _playerTwoAllSquares.Clear();
                    if (_playerTwoAllSquares.Count >= 17)
                    {
                        _playerTwoState = PlayerState.DONE;
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class Boat : List<Point>
    {
        public Boat(List<Point> boat)
            : base()
        {
            
        }
    }

    public class Point
    {
        int _x;
        int _y;

        public Point(int x = 0, int y = 0)
        {
            _x = x;
            _y = y;
        }

        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }
    }

    public enum GameState
    {
        NONE,
        SETBOATS,
        MAKETURN,
        WAIT
    }

    public enum PlayerState
    {
        WAITING,
        DONE
    }
}
