using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.ParticleMangers;

namespace YouBoss.Content.Particles
{
    public class LensFlareParticle : Particle
    {
        public float ScaleExpandRate = 0.015f;

        public override BlendState DrawBlendState => BlendState.Additive;

        public LensFlareParticle(Vector2 position, Color color, int lifetime, float scale)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
        }

        public LensFlareParticle(Vector2 position, Color color, int lifetime, float scale, float rotation)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
            Rotation = rotation;
        }

        public override void Update()
        {
            Opacity = InverseLerp(0f, 4f, Time) * InverseLerp(1f, 0.7f, LifetimeRatio);
            Scale += ScaleExpandRate;
        }
    }
}
