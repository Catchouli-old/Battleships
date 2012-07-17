using System;

namespace BattleShips
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (BattleShips game = new BattleShips())
            {
                game.Run();
            }
        }
    }
#endif
}

