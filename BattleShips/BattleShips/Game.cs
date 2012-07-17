using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BattleShips
{
    class Game
    {
        Player _player1;
        Player _player2;

        public Game(Player player1, Player player2)
        {
            _player1 = player1;
            _player2 = player2;
        }

        public override string ToString()
        {
            return _player1.ToString() + " vs. " + _player2.ToString();
        }
    }
}
