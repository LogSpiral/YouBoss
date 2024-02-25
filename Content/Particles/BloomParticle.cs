using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.ParticleMangers;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class BloomParticle : Particle
    {
        public override BlendState DrawBlendState => BlendState.Additive;

        public override string TexturePath => $"{GreyscaleTexturesPath}/BloomCircleSmall";

        public BloomParticle(Vector2 position, Color color, float scale, int lifeTime)
        {
            Position = position;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(TwoPi);
        }

        public override void Update()
        {
            Opacity = Convert01To010(LifetimeRatio);
            Lighting.AddLight(Position, Color.R / 255f, Color.G / 255f, Color.B / 255f);
        }
    }
}
