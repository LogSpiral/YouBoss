using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.ParticleMangers;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.Particles
{
    public class MetalSparkParticle : Particle
    {
        public Color GlowColor;

        public new Vector2 Scale;

        public bool AffectedByGravity;

        public static Texture2D GlowTexture
        {
            get;
            private set;
        }

        public override BlendState DrawBlendState => BlendState.Additive;

        public MetalSparkParticle(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, Vector2 scale, float opacity, Color color, Color glowColor)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Opacity = opacity;
            Lifetime = lifetime;
            Color = color;
            GlowColor = glowColor;

            if (Main.netMode != NetmodeID.Server)
                GlowTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/MetalSparkParticleGlow", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Update()
        {
            if (AffectedByGravity)
            {
                Velocity.X *= 0.9f;
                Velocity.Y += 1.1f;
            }
            Rotation = Velocity.ToRotation() + PiOver2;
            Color = Color.Lerp(Color, new(122, 108, 95), 0.06f);
            GlowColor *= 0.95f;

            Scale.X *= 0.98f;
            Scale.Y *= 0.95f;
        }

        public override void Draw()
        {
            Vector2 scale = Vector2.One * Scale;
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color * Opacity, Rotation, Texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(GlowTexture, Position - Main.screenPosition, null, GlowColor * Opacity, Rotation, GlowTexture.Size() * 0.5f, scale * 1.15f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(GlowTexture, Position - Main.screenPosition, null, GlowColor * Opacity * 0.6f, Rotation, GlowTexture.Size() * 0.5f, scale * 1.35f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(GlowTexture, Position - Main.screenPosition, null, GlowColor * Opacity * 0.3f, Rotation, GlowTexture.Size() * 0.5f, scale * 1.67f, SpriteEffects.None, 0f);
        }
    }
}
