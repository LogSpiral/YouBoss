using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Terraria;
using ReLogic.Content;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Core.Graphics.Shaders;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers
{
    public class TerraBladeSky : CustomSky
    {
        private bool skyActive;

        internal static new float Opacity;

        internal static Asset<Texture2D> BackgroundTexture;

        internal static Asset<Texture2D> MoonTexture;

        /// <summary>
        /// The identifier key for this sky.
        /// </summary>
        public const string SkyKey = "YouBoss:TerraBlade";

        public override void OnLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BackgroundTexture = ModContent.Request<Texture2D>("YouBoss/Content/NPCs/Bosses/TerraBlade/SpecificEffectManagers/Sky/Background");
            MoonTexture = ModContent.Request<Texture2D>("YouBoss/Content/NPCs/Bosses/TerraBlade/SpecificEffectManagers/Sky/Moon");
        }

        public override void Deactivate(params object[] args)
        {
            skyActive = false;
        }

        public override void Reset()
        {
            skyActive = false;
        }

        public override bool IsActive()
        {
            return skyActive || Opacity > 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            skyActive = true;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth < float.MaxValue || minDepth >= float.MaxValue)
                return;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            // Draw the background.
            Texture2D background = BackgroundTexture.Value;
            Vector2 idealSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 moonPosition = new(idealSize.X * 0.5f, 150f);
            Main.spriteBatch.Draw(background, Vector2.Zero, null, Color.White * Opacity * 0.5f, 0f, Vector2.Zero, idealSize / background.Size(), 0, 0f);

            // Make Terraria's vanilla stars disappear.
            for (int i = 0; i < Main.maxStars; i++)
                Main.star[i] = new();

            // Draw an assortment of custom stars.
            ulong starSeed = 1493uL;
            float starCountFractional = 60f;
            if (TerraBladeBoss.Myself is not null)
                starCountFractional += TerraBladeBoss.Myself.As<TerraBladeBoss>().ExtraStarsInSkyCount;

            float strengthOfLastStar = starCountFractional % 1f;
            int starCount = (int)Ceiling(starCountFractional);
            for (int i = 0; i < starCount; i++)
            {
                bool lastStart = i == starCount - 1;

                float starX = Lerp(150f, idealSize.X - 150f, Utils.RandomFloat(ref starSeed));
                float starY = Lerp(50f, idealSize.Y * 0.48f, Pow(Utils.RandomFloat(ref starSeed), 2.2f));
                float starSize = Lerp(0.2f, 0.85f, Utils.RandomFloat(ref starSeed).Cubed() * (lastStart ? strengthOfLastStar : 1f));
                float starColorInterpolant = Utils.RandomFloat(ref starSeed);
                float starFlarePulse = Lerp(0.85f, 1.2f, Cos01(Main.GlobalTimeWrappedHourly * 2.4f + i * 2.3f));
                Color starColor = MulticolorLerp(starColorInterpolant, Color.Cyan, Color.Teal, Color.LightGoldenrodYellow);
                starColor.A = 0;
                Color brightStarColor = Color.Lerp(starColor, Color.White with { A = 0 }, 0.5f);

                if (lastStart)
                    starSize *= Lerp(1f, 2.3f, InverseLerpBump(0f, 0.32f, 0.51f, 1f, strengthOfLastStar).Squared());

                Vector2 starDrawPosition = new(starX, starY);
                starDrawPosition -= (moonPosition - starDrawPosition).SafeNormalize(Vector2.Zero) * 20f;

                Main.spriteBatch.Draw(BloomCircleSmall, starDrawPosition, null, starColor * Opacity * 0.75f, 0f, BloomCircleSmall.Size() * 0.5f, starSize / starFlarePulse * 0.2f, 0, 0f);
                Main.spriteBatch.Draw(BloomCircleSmall, starDrawPosition, null, brightStarColor * Opacity, 0f, BloomCircleSmall.Size() * 0.5f, starSize / starFlarePulse * 0.1f, 0, 0f);
                Main.spriteBatch.Draw(ShineFlareTexture, starDrawPosition, null, brightStarColor * Opacity, 0f, ShineFlareTexture.Size() * 0.5f, starSize * starFlarePulse * 0.1f, 0, 0f);
            }

            // Draw a bit of bloom behind the moon.
            Main.spriteBatch.Draw(BloomCircleSmall, moonPosition, null, new Color(57, 190, 206, 0) * Opacity * 0.4f, 0f, BloomCircleSmall.Size() * 0.5f, 4f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, moonPosition, null, new Color(57, 230, 160, 0) * Opacity * 0.2f, 0f, BloomCircleSmall.Size() * 0.5f, 8f, 0, 0f);

            // Draw the moon.
            Texture2D moon = MoonTexture.Value;
            Main.spriteBatch.Draw(moon, moonPosition, null, Color.White * Opacity, 0f, moon.Size() * 0.5f, 0.25f, 0, 0f);

            // Draw a bit of bloom over the moon.
            Main.spriteBatch.Draw(BloomCircleSmall, moonPosition, null, new Color(124, 255, 222, 0) * Opacity * 0.2f, 0f, BloomCircleSmall.Size() * 0.5f, 1.5f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, moonPosition, null, new Color(74, 255, 172, 0) * Opacity * 0.2f, Main.GlobalTimeWrappedHourly * -0.054f, BloomFlare.Size() * 0.5f, 0.7f, 0, 0f);

            // Draw bloom far below the moon as an indicator of a bright reflection.
            if (Main.BackgroundEnabled)
            {
                float verticalOffset = idealSize.Y - 100f;
                Main.spriteBatch.Draw(BloomCircleSmall, moonPosition + Vector2.UnitY * verticalOffset, null, new Color(228, 228, 238, 0) * Opacity * 0.6f, 0f, BloomCircleSmall.Size() * 0.5f, 9f, 0, 0f);
                Main.spriteBatch.Draw(BloomCircleSmall, moonPosition + Vector2.UnitY * verticalOffset, null, new Color(255, 255, 152, 0) * Opacity * 0.4f, 0f, BloomCircleSmall.Size() * 0.5f, 16f, 0, 0f);
                Main.spriteBatch.Draw(BloomFlare, moonPosition + Vector2.UnitY * verticalOffset, null, new Color(255, 255, 255, 0) * Opacity * 0.51f, Main.GlobalTimeWrappedHourly * 0.051f, BloomFlare.Size() * 0.5f, 1.6f, 0, 0f);
            }

            // Draw the aurora.
            DrawAurora();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
        }

        public static void DrawAurora()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            // Prepare the aurora shader.
            float verticalSquish = 0.75f;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            var auroraShader = ShaderManager.GetShader("AuroraShader");
            auroraShader.TrySetParameter("verticalSquish", 0.4f);
            auroraShader.TrySetParameter("scrollSpeedFactor", 0.05f);
            auroraShader.TrySetParameter("accentApplicationStrength", 2.81f);
            auroraShader.TrySetParameter("parallaxOffset", Main.LocalPlayer.Center / screenArea * 1.5f);
            auroraShader.TrySetParameter("bottomAuroraColor", new Vector3(1.32f, 0.4f, 1.4f));
            auroraShader.TrySetParameter("topAuroraColor", new Vector3(0f, 1.34f, 1.06f));
            auroraShader.TrySetParameter("auroraColorAccent", new Vector3(0.16f, -0.4f, 0.21f));
            auroraShader.SetTexture(TurbulentNoise, 1, SamplerState.AnisotropicWrap);
            auroraShader.Apply();

            // Draw the texture. The shader will use it to draw the actual aurora.
            Vector2 auroraDrawPosition = screenArea * new Vector2(0.5f, 0f);
            if (Main.gameMenu)
                auroraDrawPosition.Y += 100f;

            Main.spriteBatch.Draw(DendriticNoise, auroraDrawPosition, null, Color.White * Opacity * 0.435f, 0f, DendriticNoise.Size() * new Vector2(0.5f, 0f), screenArea / DendriticNoise.Size() * new Vector2(1f, verticalSquish), 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);
        }

        public override void Update(GameTime gameTime)
        {
            if (Main.gameMenu)
                skyActive = false;

            Opacity = Saturate(Opacity + skyActive.ToDirectionInt() * 0.1f);
        }

        public override float GetCloudAlpha() => 1f - Opacity;
    }
}
