using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Common.Tools.DataStructures;
using YouBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles
{
    public class TerraGroundShock : ModProjectile, IDrawAdditive, IProjOwnedByBoss<TerraBladeBoss>
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
        }

        public override void AI()
        {
            Projectile.Opacity = InverseLerp(0f, 11f, Projectile.timeLeft);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Texture2D lightning = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * 24f;

            // Draw a purple backglow.
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, new Color(174, 206, 64) * Projectile.Opacity, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, new Color(53, 165, 101) * Projectile.Opacity * 0.67f, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 3f, 0, 0f);
            Main.spriteBatch.ResetToDefault();

            // Draw strong bluish pink lightning zaps above the ground.
            // The DrawOverTiles method will ensure that said lightning zaps do not draws where there are no tiles.
            ulong lightningSeed = (ulong)Projectile.identity * 772496uL;
            for (int i = 0; i < 6; i++)
            {
                Vector2 lightningScale = new Vector2(1.2f, 1.6f) * Projectile.scale * Lerp(0.3f, 0.5f, Utils.RandomFloat(ref lightningSeed)) * 1.9f;
                float lightningRotation = Lerp(-1.04f, 1.04f, i / 4f + Utils.RandomFloat(ref lightningSeed) * 0.1f) + PiOver2;
                Color lightningColor = Color.Lerp(Color.Yellow, Color.Turquoise, Utils.RandomFloat(ref lightningSeed)) * Projectile.Opacity * 0.4f;
                lightningColor.A = 0;

                Main.spriteBatch.Draw(lightning, drawPosition, null, lightningColor, lightningRotation, lightning.Size() * Vector2.UnitY * 0.5f, lightningScale, 0, 0f);
                Main.spriteBatch.Draw(lightning, drawPosition, null, lightningColor * 0.3f, lightningRotation, lightning.Size() * Vector2.UnitY * 0.5f, lightningScale * new Vector2(1f, 1.1f), 0, 0f);
            }
        }
    }
}
