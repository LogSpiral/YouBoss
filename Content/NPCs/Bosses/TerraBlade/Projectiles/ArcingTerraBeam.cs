using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Common.Tools.DataStructures;
using YouBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles
{
    public class ArcingTerraBeam : ModProjectile, IDrawAdditive, IProjOwnedByBoss<TerraBladeBoss>
    {
        public ref float ArcAngularVelocity => ref Projectile.ai[0];

        public ref float MaxSpeedBoost => ref Projectile.ai[1];

        public ref float Time => ref Projectile.ai[2];

        public override string Texture => "YouBoss/Content/NPCs/Bosses/TerraBlade/Projectiles/TerraBeam";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Fade out prior to dying.
            Projectile.Opacity = InverseLerp(0f, 11f, Projectile.timeLeft);
            if (Projectile.Opacity <= 0.6f)
                Projectile.damage = 0;

            // Accelerate and arc.
            float maxSpeed = MaxSpeedBoost + 22f;
            float acceleration = InverseLerp(0f, 60f, Time).Squared() * 0.7f;
            float newSpeed = Clamp(Projectile.velocity.Length() + acceleration, 0f, maxSpeed);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(ArcAngularVelocity) * newSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

            // Create terra dust.
            if (Main.rand.NextBool(3))
            {
                Color terraColor = Main.hslToRgb(Main.rand.NextFloat(0.35f, 0.43f), 1f, 0.85f);
                Dust terraDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f) + Projectile.velocity, 267, Projectile.velocity * -0.4f, 0, terraColor);
                terraDust.scale = 0.36f;
                terraDust.fadeIn = Main.rand.NextFloat(1.2f);
                terraDust.noGravity = true;
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, Vector3.One * 0.7f);

            // Increment time.
            Time++;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Color color = Color.Lerp(Color.White, Color.Orange, Projectile.identity / 11f % 1f);
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], color, positionClumpInterpolant: 0.6f);
        }
    }
}
