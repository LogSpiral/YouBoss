using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.Items.ItemReworks
{
    public class HomingTerraBeam : ModProjectile, IDrawAdditive
    {
        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "YouBoss/Content/NPCs/Bosses/TerraBlade/Projectiles/TerraBeam";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Fade out prior to dying, and fade in as the beam materializes.
            Projectile.Opacity = InverseLerp(0f, 6f, Time) * InverseLerp(0f, 32f, Projectile.timeLeft);
            if (Projectile.timeLeft <= 16)
                Projectile.damage = 0;

            // Slow down rapidly at first.
            if (Time <= 15f)
                Projectile.velocity *= 0.6f;

            // Make the time go by a lot quicker if moving slowly after the initial slowdown.
            if (Time >= 20f && Projectile.velocity.Length() <= 8f)
                Projectile.timeLeft -= 3;

            // Rapidly fly towards the nearest target.
            NPC potentialTarget = Projectile.FindTargetWithinRange(FirstFractal.HomingBeamSearchRange);
            if (Time >= 20f && potentialTarget is not null && potentialTarget.active)
            {
                Vector2 idealVelocity = Projectile.DirectionToSafe(potentialTarget.Center) * (Projectile.velocity.Length() + 12f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.09f);
            }

            // Define rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

            // Create terra dust.
            if (Main.rand.NextBool(4))
            {
                Color terraColor = Main.hslToRgb(Main.rand.NextFloat(0.35f, 0.45f), 1f, 0.82f);
                Dust terraDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f) + Projectile.velocity, 261, Projectile.velocity * -0.3f, 0, terraColor);
                terraDust.scale = 0.75f;
                terraDust.fadeIn = Main.rand.NextFloat(1.3f);
                terraDust.noGravity = true;
            }

            // Emit light.
            Lighting.AddLight(Projectile.Center, Vector3.One * 0.7f);

            // Increment time.
            Time++;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Color color = Color.Lerp(Color.White, Color.Orange, Projectile.identity / 12f % 1f);
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], color, positionClumpInterpolant: 0.56f);
        }
    }
}
