using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BattleShips
{
    class Player : IComparable
    {
        int _id;
        string _name;

        public Player(int id = 0, string name = "Player")
        {
            _id = id;
            _name = name;
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

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public int CompareTo(object obj)
        {
            if (ID < ((Player)obj).ID)
                return 1;
            else
                return -1;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
