using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using YouBoss.Core.Graphics.Automators;

namespace YouBoss.Content.Items.ItemReworks
{
    public class TerraSlash : ModProjectile, IDrawAdditive
    {
        /// <summary>
        /// How long the slash has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// How long the slash lingers for.
        /// </summary>
        public static int Lifetime => SecondsToFrames(0.6f);

        public override void SetDefaults()
        {
            Projectile.width = 656;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 3;
            Projectile.scale = 0.71f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 10;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Opacity = Sqrt(1f - Time / Lifetime);

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override bool ShouldUpdatePosition() => true;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Time <= 2f)
                return false;

            Main.spriteBatch.UseBlendState(SubtractiveBlending);
            DrawWithColor(Color.White, Color.Transparent);
            Main.spriteBatch.ResetToDefault();

            return false;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Color slashColor = Color.Lerp(Color.Teal, Color.Cyan, Projectile.identity / 12f % 1f);
            slashColor.R += 100;

            DrawWithColor(Color.White * 0.4f, slashColor);
            DrawWithColor(Color.Transparent, slashColor * 0.81f);
        }

        public void DrawWithColor(Color bloomColor, Color slashColor)
        {
            float animationCompletion = Time / Lifetime;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = BloomCircleSmall;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 scale = new Vector2(Lerp(0.6f, 1.1f, Pow(animationCompletion, 0.45f)), 0.55f - Pow(animationCompletion, 0.4f) * 0.32f) * Projectile.scale;
            scale *= InverseLerp(0f, 0.36f, animationCompletion).Squared();

            Vector2 bloomScale = Projectile.Size / bloomTexture.Size() * new Vector2(1f, 3f);
            Vector2 bloomOrigin = bloomTexture.Size() * 0.5f;
            Main.spriteBatch.Draw(bloomTexture, drawPosition, null, bloomColor * Projectile.Opacity, Projectile.rotation, bloomOrigin, bloomScale, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(slashColor), Projectile.rotation, origin, scale, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, null, bloomColor * Projectile.Opacity, Projectile.rotation, origin, scale * new Vector2(1f, 0.6f), 0, 0f);
        }
    }
}
