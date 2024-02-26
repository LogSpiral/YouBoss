using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.ParticleMangers;
using Terraria;

namespace YouBoss.Content.Particles
{
    public class FallingBloomParticle : Particle
    {
        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => $"{GreyscaleTexturesPath}/BloomCircleSmall";

        public FallingBloomParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
            Opacity = 1f;
        }

        public override void Update()
        {
            if (Time == 4)
            {
                Velocity.X *= 0.04f;
                Velocity.Y = Lerp(0.1f, 0.38f, Main.rand.NextFloat().Cubed());
            }

            if (Time >= 4)
            {
                Velocity.X *= 0.98f;
                Scale *= Remap(Time - Lifetime, -150f, -25f, 1f, 0.96f);
            }
        }

        public override void Draw()
        {
            float scalePulse = Cos(Main.GlobalTimeWrappedHourly * 7.05f + Velocity.Y * 30f) * Scale * 0.25f;
            float scale = Scale + InverseLerp(0.8f, 0.4f, Scale) * scalePulse;
            Rectangle frame = Texture.Frame(1, FrameCount, 0, Frame);
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color * Opacity, Rotation, frame.Size() * 0.5f, scale, 0, 0f);
            Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, frame, Color.White * Opacity * 0.7f, Rotation, frame.Size() * 0.5f, scale * 0.67f, 0, 0f);
        }
    }
}
