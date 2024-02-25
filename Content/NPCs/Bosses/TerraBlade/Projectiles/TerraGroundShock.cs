using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Tools.DataStructures;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade.Projectiles
{
    public class TerraGroundShock : ModProjectile, IDrawsOverTiles, IProjOwnedByBoss<TerraBladeBoss>
    {
        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Projectile.Opacity = InverseLerp(0f, 11f, Projectile.timeLeft);
        }

        public void DrawOverTiles(SpriteBatch spriteBatch)
        {
            spriteBatch.UseBlendState(BlendState.Additive);

            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * 24f;

            // Draw a purple backglow.
            spriteBatch.Draw(BloomCircle, drawPosition, null, new Color(174, 206, 64) * Projectile.Opacity, 0f, BloomCircle.Size() * 0.5f, Projectile.scale * 0.8f, 0, 0f);
            spriteBatch.Draw(BloomCircle, drawPosition, null, new Color(53, 165, 101) * Projectile.Opacity * 0.67f, 0f, BloomCircle.Size() * 0.5f, Projectile.scale * 2f, 0, 0f);
            spriteBatch.ResetToDefault();

            // Draw strong bluish pink lightning zaps above the ground.
            // The DrawOverTiles method will ensure that said lightning zaps do not draws where there are no tiles.
            ulong lightningSeed = (ulong)Projectile.identity * 772496uL;
            for (int i = 0; i < 6; i++)
            {
                Vector2 lightningScale = new Vector2(1.2f, 1.6f) * Projectile.scale * Lerp(0.3f, 0.5f, Utils.RandomFloat(ref lightningSeed)) * 1.9f;
                float lightningRotation = Lerp(-1.04f, 1.04f, i / 4f + Utils.RandomFloat(ref lightningSeed) * 0.1f) + PiOver2;
                Color lightningColor = Color.Lerp(Color.Yellow, Color.Turquoise, Utils.RandomFloat(ref lightningSeed)) * Projectile.Opacity * 0.4f;
                lightningColor.A = 0;

                spriteBatch.Draw(StreakLightning, drawPosition, null, lightningColor, lightningRotation, StreakLightning.Size() * Vector2.UnitY * 0.5f, lightningScale, 0, 0f);
                spriteBatch.Draw(StreakLightning, drawPosition, null, lightningColor * 0.3f, lightningRotation, StreakLightning.Size() * Vector2.UnitY * 0.5f, lightningScale * new Vector2(1f, 1.1f), 0, 0f);
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;
    }
}
