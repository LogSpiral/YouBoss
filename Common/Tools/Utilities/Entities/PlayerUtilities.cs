using Terraria;

namespace YouBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Gives a given <see cref="Player"/> infinite flight.
        /// </summary>
        /// <param name="p">The player to apply infinite flight to.</param>
        public static void GrantInfiniteFlight(this Player p)
        {
            p.wingTime = p.wingTimeMax;
        }
    }
}
