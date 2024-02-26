using Terraria.Audio;

namespace YouBoss.Assets
{
    /// <summary>
    /// Acts as a centralized storage for sounds where manual storing causes significant overhead.
    /// </summary>
    public static class SoundsRegistry
    {
        /// <summary>
        /// Holds common, generic-use sound assets across the mod.
        /// </summary>
        public static class Common
        {
            public const string DirectoryBase = "YouBoss/Assets/Sounds/Custom/Common";

            /// <summary>
            /// A generic twinkle sound. Often used alongside a twinkle particle in some way.
            /// </summary>
            public static readonly SoundStyle TwinkleSound = new SoundStyle($"{DirectoryBase}/Twinkle") with { MaxInstances = 5, PitchVariance = 0.16f };
        }

        /// <summary>
        /// Holds sounds specialized for use by terra blade boss.
        /// </summary>
        public static class TerraBlade
        {
            /// <summary>
            /// The sound the blade makes when reforming from its three components.
            /// </summary>
            public static readonly SoundStyle BladeReformExplosionSound = new("YouBoss/Assets/Sounds/Custom/TerraBlade/BladeReformExplosion");

            /// <summary>
            /// The sound the blade makes when dashing.
            /// </summary>
            public static readonly SoundStyle DashSound = new("YouBoss/Assets/Sounds/Custom/TerraBlade/Dash");

            /// <summary>
            /// The sound the blade makes when the player disappears.
            /// </summary>
            public static readonly SoundStyle PlayerDisappearSound = new("YouBoss/Assets/Sounds/Custom/TerraBlade/PlayerDisappear");

            /// <summary>
            /// The sound the blade makes when splitting into its three components.
            /// </summary>
            public static readonly SoundStyle SplitSound = new("YouBoss/Assets/Sounds/Custom/TerraBlade/Split");

            /// <summary>
            /// The sound the blade makes when slashing.
            /// </summary>
            public static readonly SoundStyle SlashSound = new("YouBoss/Assets/Sounds/Custom/TerraBlade/Slash");
        }
    }
}
