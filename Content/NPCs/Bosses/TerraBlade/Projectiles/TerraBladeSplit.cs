using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Common.Tools.DataStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles
{
    public class TerraBladeSplit : ModProjectile, IProjOwnedByBoss<TerraBladeBoss>
    {
        public enum BladeVariant
        {
            BrokenHeroSword,
            TrueNightsEdge,
            TrueExcalibur
        }

        /// <summary>
        /// The form variant of this split blade.
        /// </summary>
        public BladeVariant Variant
        {
            get => (BladeVariant)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        /// <summary>
        /// That amount of time has has passed since this blade was spawned, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[1];

        public override string Texture
        {
            get
            {
                int itemID = ItemID.None;
                switch (Variant)
                {
                    case BladeVariant.BrokenHeroSword:
                        itemID = ItemID.BrokenHeroSword;
                        break;
                    case BladeVariant.TrueNightsEdge:
                        itemID = ItemID.TrueNightsEdge;
                        break;
                    case BladeVariant.TrueExcalibur:
                        itemID = ItemID.TrueExcalibur;
                        break;
                }

                return $"Terraria/Images/Item_{itemID}";
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
        }

        public override void AI()
        {
            // Emit light.
            Lighting.AddLight(Projectile.Center, Vector3.One * 0.8f);

            // Increment time.
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White, texture: texture, positionClumpInterpolant: 0.6f);
            return false;
        }
    }
}
