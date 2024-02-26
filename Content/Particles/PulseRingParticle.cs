using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.ParticleMangers;

namespace YouBoss.Content.Particles
{
    public class PulseRingParticle : Particle
    {
        /// <summary>
        /// The starting scale of the ring.
        /// </summary>
        public float StartingScale;

        /// <summary>
        /// The ending scale of the ring.
        /// </summary>
        public float EndingScale;

        public override BlendState DrawBlendState => BlendState.Additive;

        public PulseRingParticle(Vector2 position, Color color, float originalScale, float finalScale, int lifeTime)
        {
            Position = position;
            Color = color;
            StartingScale = originalScale;
            EndingScale = finalScale;
            Scale = originalScale;
            Lifetime = lifeTime;
        }

        public override void Update()
        {
            Scale = Lerp(StartingScale, EndingScale, Pow(LifetimeRatio, 1.5f));
            Opacity = InverseLerpBump(0f, 0.1f, 0.56f, 1f, LifetimeRatio);
        }
    }
}
