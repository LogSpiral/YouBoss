using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Tools.DataStructures;
using NoxusBoss.Core.Graphics.Automators;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade.Projectiles
{
    public class TelegraphedTerraBeam : ModProjectile, IDrawAdditive, IProjOwnedByBoss<TerraBladeBoss>
    {
        /// <summary>
        /// The appear interpolant for this beam.
        /// </summary>
        public float BeamAppearInterpolant => InverseLerp(TelegraphTime - 16f, TelegraphTime - 3f, Time);

        /// <summary>
        /// Whether this beam can create telegraph sounds or not.
        /// </summary>
        public bool CanPlaySounds => Projectile.ai[1] == 1f;

        /// <summary>
        /// How long this beam should telegraph for, in frames.
        /// </summary>
        public ref float TelegraphTime => ref Projectile.ai[0];

        /// <summary>
        /// How long this beam has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[2];

        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public override string Texture => "NoxusBoss/Content/NPCs/Bosses/TerraBlade/Projectiles/TerraBeam";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;

            if (Main.netMode != NetmodeID.Server)
                MyTexture = ModContent.Request<Texture2D>(Texture);
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;

            // Increased so that the graze checks are more precise.
            Projectile.MaxUpdates = 2;

            Projectile.timeLeft = Projectile.MaxUpdates * 120;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Sharply fade in.
            Projectile.Opacity = InverseLerp(0f, 12f, Time);

            // Decide rotation based on direction.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

            // Accelerate after the telegraph dissipates.
            if (Time >= TelegraphTime)
            {
                float newSpeed = Clamp(Projectile.velocity.Length() + 5f / Projectile.MaxUpdates, 14f, 90f / Projectile.MaxUpdates);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
            }

            // Create terra blade particles.
            if (Main.rand.NextBool(10))
            {
                ParticleOrchestraSettings particleSettings = new()
                {
                    PositionInWorld = Projectile.Center,
                    MovementVector = Main.rand.NextVector2Circular(7f, 7f)
                };
                ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.TerraBlade, particleSettings);
            }

            // Play the ordinary graze slice sound.
            if (Time == TelegraphTime + 9f && CanPlaySounds)
            {
                //SoundEngine.PlaySound(GrazeSound with { Volume = 0.6f, MaxInstances = 20 });
                StartShake(4f);
            }

            if (Projectile.IsFinalExtraUpdate())
                Time++;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            if (Time <= TelegraphTime)
                DrawTelegraph();

            // Draw bloom underneath the beam. This is strongest when it has not yet fully faded in.
            float bloomOpacity = Lerp(0.9f, 0.51f, BeamAppearInterpolant) * Projectile.Opacity;
            Color mainColor = new Color(167, 234, 79) * bloomOpacity;
            Color secondaryColor = new Color(15, 203, 132) * bloomOpacity;

            Main.EntitySpriteDraw(BloomCircleSmall, Projectile.Center - Main.screenPosition, null, mainColor, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 1.32f, 0, 0);
            Main.EntitySpriteDraw(BloomCircleSmall, Projectile.Center - Main.screenPosition, null, secondaryColor, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 0.6f, 0, 0);

            // Make the beam appear near the end of the telegraph fade-in.
            float beamOffsetFactor = Projectile.velocity.Length() * 0.4f;
            Texture2D beamTexture = MyTexture.Value;
            for (int i = 0; i < 30; i++)
            {
                float beamScale = Lerp(1f, 0.48f, i / 29f) * Projectile.scale;
                Vector2 beamDrawOffset = Projectile.velocity.SafeNormalize(Vector2.UnitY) * BeamAppearInterpolant * i * beamScale * -beamOffsetFactor;
                Color beamDrawColor = mainColor * BeamAppearInterpolant * Pow(1f - i / 10f, 1.6f) * Projectile.Opacity * 1.8f;
                Main.EntitySpriteDraw(beamTexture, Projectile.Center + beamDrawOffset - Main.screenPosition, null, beamDrawColor, Projectile.rotation, beamTexture.Size() * 0.5f, beamScale, 0, 0);
            }
        }

        public void DrawTelegraph()
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3100f;
            Main.spriteBatch.DrawBloomLine(start, end, new Color(15, 203, 132) * Sqrt(1f - BeamAppearInterpolant), Projectile.Opacity * 40f);
        }

        public override bool? CanDamage() => Time >= TelegraphTime;

        public override bool ShouldUpdatePosition() => Time >= TelegraphTime;
    }
}
