using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.Items.ItemReworks
{
    public class FirstFractal : ModItem
    {
        /// <summary>
        /// The amount of time spent per sword swing, in frames, accounting for max updates.
        /// </summary>
        public static int UseTime => FirstFractalHoldout.MaxUpdates * SecondsToFrames(0.55f);

        /// <summary>
        /// The base damage of the sword.
        /// </summary>
        public static int BaseDamage => 1250;

        /// <summary>
        /// How far homing terra beams can search to find a potential target.
        /// </summary>
        public static float HomingBeamSearchRange => 1372f;

        /// <summary>
        /// The damage factor for the projectile beams relative to the sword's damage.
        /// </summary>
        public static float HomingBeamDamageFactor => 0.2f;

        /// <summary>
        /// The damage factor for the projectile slashes relative to the sword's damage.
        /// </summary>
        public static float HomingSlashDamageFactor => 0.4f;

        /// <summary>
        /// The acceleration of the homing terra beams.
        /// </summary>
        public static float HomingBeamAcceleration => 1.38f;

        /// <summary>
        /// The speed interpolant of the homing terra beams.
        /// </summary>
        public static float HomingBeamFlySpeedInterpolant => 0.09f;

        /// <summary>
        /// How fast the player should go after hitting an enemy with the super dash.
        /// </summary>
        public static float PlayerPostHitSpeed => 30f;

        /// <summary>
        /// How many immunity frames the player is afforded upon hitting an enemy with the super dash.
        /// </summary>
        public static int PlayerPostHitIFrameGracePeriod => SecondsToFrames(0.8f);

        /// <summary>
        /// The horizontal speed of the player during the super dash.
        /// </summary>
        public static float PlayerHorizontalDashSpeed => 95f;

        /// <summary>
        /// The starting speed of homing terra beams upon being spawned.
        /// </summary>
        public static float HomingBeamStartingSpeed => 250f;

        /// <summary>
        /// The deceleration factor homing terra beams shortly after they spawn in.
        /// </summary>
        public static float HomingBeamDecelerationFactor => 0.6f;

        public override void SetDefaults()
        {
            Item.width = 88;
            Item.height = 88;
            Item.damage = BaseDamage;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 11;
            Item.useAnimation = 11;
            Item.useTurn = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.UseSound = null;
            Item.knockBack = 8f;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<FirstFractalHoldout>();
            Item.shootSpeed = 9f;
            Item.rare = ItemRarityID.Purple;
        }

        public override bool CanUseItem(Player player)
        {
            if (Item.type != ItemID.FirstFractal)
                return true;

            return player.ownedProjectileCounts[Item.shoot] <= 0;
        }
    }
}
