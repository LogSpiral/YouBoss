using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.Primitives;
using YouBoss.Core.Graphics.Shaders;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    [AutoloadBossHead()]
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The offset for the terra blade's draw origin.
        /// </summary>
        public Vector2 OriginOffset
        {
            get;
            set;
        }

        /// <summary>
        /// The completion interpolant of the dash afterimage.
        /// </summary>
        public float AfterimageTrailCompletion
        {
            get;
            set;
        }

        /// <summary>
        /// The scale of the terra blade's back image.
        /// </summary>
        public float BackImageScale
        {
            get;
            set;
        }

        /// <summary>
        /// The opacity of the terra blade's back image.
        /// </summary>
        public float BackImageOpacity
        {
            get;
            set;
        }

        /// <summary>
        /// The amount by which the player has materialized.
        /// </summary>
        public float PlayerAppearanceInterpolant
        {
            get;
            set;
        }

        /// <summary>
        /// The draw offset factor for the player holding the terra blade.
        /// </summary>
        public float PlayerDrawOffsetFactor
        {
            get;
            set;
        } = 1f;

        /// <summary>
        /// The primitive trail responsible for drawing dash afterimages for the terra blade.
        /// </summary>
        public PrimitiveTrail DashAfterimageTrail
        {
            get;
            set;
        }

        /// <summary>
        /// The render target that holds all player render data.
        /// </summary>
        public static TerraBladePlayerTargetContent PlayerDrawContents
        {
            get;
            private set;
        }

        /// <summary>
        /// The intensity of the terra blade's shine.
        /// </summary>
        public ref float ShineInterpolant => ref NPC.localAI[0];

        /// <summary>
        /// How much positional afterimages from the terra blade should clump.
        /// </summary>
        public ref float AfterimageClumpInterpolant => ref NPC.localAI[1];

        /// <summary>
        /// The opacity of the terra blade's afterimages.
        /// </summary>
        public ref float AfterimageOpacity => ref NPC.localAI[2];

        /// <summary>
        /// How much the terra blade should be squished.
        /// </summary>
        public ref float VerticalSquish => ref NPC.localAI[3];

        /// <summary>
        /// The terra blade's shine overlay texture.
        /// </summary>
        internal static Asset<Texture2D> ShineTexture;

        public static void LoadTextures()
        {
            // Don't attempt to load textures sever-side.
            if (Main.netMode == NetmodeID.Server)
                return;

            ShineTexture = ModContent.Request<Texture2D>("YouBoss/Content/NPCs/Bosses/TerraBlade/TerraBladeShine");
        }

        public void DrawAntiHero(Vector2 drawOffset, Color lightColor)
        {
            // Initialize the player drawer, with the terra blade as its current host.
            PlayerDrawContents.Host = this;
            PlayerDrawContents.Request();

            // If the drawer isn't ready, wait until it is.
            if (!PlayerDrawContents.IsReady)
                return;

            // Draw the player.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw a glow behind the player based on their appearance interpolant.
            float backglowOpacity = InverseLerpBump(0f, 0.15f, 0.5f, 1f, PlayerAppearanceInterpolant);
            Vector2 drawPosition = NPC.Center - (NPC.rotation + (NPC.spriteDirection == -1 ? -PiOver2 : 0f)).ToRotationVector2() * PlayerDrawOffsetFactor * 26f + drawOffset;
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, new Color(12, 255, 255, 0) * backglowOpacity, 0f, BloomCircleSmall.Size() * 0.5f, 1f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, new Color(12, 185, 161, 0) * backglowOpacity * 0.6f, 0f, BloomCircleSmall.Size() * 0.5f, 1.7f, 0, 0f);

            // Apply the appearance shader.
            ManagedShader appearanceShader = ShaderManager.GetShader("TerraBladePlayerAppearShader");
            appearanceShader.TrySetParameter("appearanceInterpolant", PlayerAppearanceInterpolant);
            appearanceShader.TrySetParameter("blendColor", new Vector3(0f, 1.05f, 0.85f));
            appearanceShader.SetTexture(WavyBlotchNoise, 1, SamplerState.PointWrap);
            appearanceShader.Apply();

            Texture2D target = PlayerDrawContents.GetTarget();
            DrawData targetData = new(target, drawPosition, null, NPC.GetAlpha(lightColor), 0f, target.Size() * 0.5f, Vector2.One / Main.GameViewMatrix.Zoom, 0, 0f);
            targetData.Draw(Main.spriteBatch);

            if (NPC.IsABestiaryIconDummy)
                Main.spriteBatch.ResetToDefaultUI();
            else
                Main.spriteBatch.ResetToDefault();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Calculate general-purpose draw variables.
            float bladeRotation = NPC.rotation + PiOver4;
            Texture2D bladeTexture = ModContent.Request<Texture2D>($"Terraria/Images/Item_{ItemID.TerraBlade}").Value;
            Vector2 originOffsetInPixels = OriginOffset * bladeTexture.Size();
            Vector2 drawPosition = NPC.Center - screenPos - Vector2.UnitY * originOffsetInPixels;
            Vector2 scale = new Vector2(1f, 1f - VerticalSquish) * NPC.scale;
            Vector2 origin = bladeTexture.Size() * 0.5f + originOffsetInPixels;
            SpriteEffects bladeDirection = NPC.spriteDirection.ToSpriteDirection();

            // Draw the antihero.
            DrawAntiHero(Vector2.UnitY * originOffsetInPixels - screenPos, drawColor);

            // Draw the afterimage trail behind everything if necessary.
            DrawAfterimageTrail();

            // Draw a back-image if necessary.
            if (BackImageOpacity > 0f)
                Main.EntitySpriteDraw(bladeTexture, drawPosition, null, NPC.GetAlpha(Color.White) * BackImageOpacity, bladeRotation, origin, BackImageScale, bladeDirection);

            // Draw the terra blade.
            Main.EntitySpriteDraw(bladeTexture, drawPosition, null, NPC.GetAlpha(Color.White), bladeRotation, origin, scale, bladeDirection);

            // Draw afterimages.
            Texture2D shineTexture = ShineTexture.Value;
            if (AfterimageOpacity > 0f)
            {
                for (int i = 9; i >= 0; i--)
                {
                    if (NPC.oldPos[i] == Vector2.Zero)
                        continue;

                    // Draw the afterimage.
                    float afterimageOpacity = InverseLerp(10f, 0f, i) * AfterimageOpacity;
                    float afterimageRotation = NPC.oldRot[i].AngleLerp(NPC.rotation, AfterimageClumpInterpolant * 0.35f) + PiOver4;
                    Vector2 afterimageDrawOffset = (NPC.oldPos[i] - NPC.position) * (1f - AfterimageClumpInterpolant);
                    Main.EntitySpriteDraw(bladeTexture, drawPosition + afterimageDrawOffset, null, NPC.GetAlpha(Color.White) * afterimageOpacity, afterimageRotation, origin, scale, bladeDirection);

                    // Draw the blade shine.
                    Main.EntitySpriteDraw(shineTexture, drawPosition + afterimageDrawOffset, null, NPC.GetAlpha(Color.White with { A = 0 }) * afterimageOpacity * 0.5f, afterimageRotation, origin, scale, bladeDirection);
                }
            }

            // Draw a shine over the blade.
            if (ShineInterpolant > 0f)
            {
                Color shineColor = NPC.GetAlpha(Color.White) * ShineInterpolant;
                shineColor.A = 0;

                // Draw the blade shine.
                Main.EntitySpriteDraw(shineTexture, drawPosition, null, shineColor, bladeRotation, origin, scale, bladeDirection);

                // Draw the shine sparkle.
                shineColor = NPC.GetAlpha(new(179, 249, 147)) * ShineInterpolant.Squared();
                shineColor.A = 0;

                Vector2 pointDirection = NPC.rotation.ToRotationVector2();
                if (NPC.spriteDirection == -1)
                    pointDirection = pointDirection.RotatedBy(-PiOver2);

                float shineScale = Lerp(0.18f, 0.32f, Sin01(NPC.Center.X * 0.15f + Main.GlobalTimeWrappedHourly * 1.9f));
                Vector2 shineDrawPosition = NPC.Center + pointDirection * NPC.scale * 24f - screenPos;
                Main.EntitySpriteDraw(ShineFlareTexture, shineDrawPosition, null, shineColor, 0f, ShineFlareTexture.Size() * 0.5f, ShineInterpolant * shineScale, 0);
            }

            return false;
        }

        public void DrawAfterimageTrail()
        {
            // Don't do anything if afterimages should not be drawn.
            if (AfterimageTrailCompletion <= 0f || AfterimageTrailCompletion >= 1f)
                return;

            // Prepare the afterimage shader.
            var afterimageShader = ShaderManager.GetShader("TerraBladeDashShader");
            afterimageShader.SetTexture(DendriticNoiseZoomedOut, 1, SamplerState.LinearWrap);

            DashAfterimageTrail = new(DashAfterimageWidthFunction, DashAfterimageColorFunction, null, true, afterimageShader);
            DashAfterimageTrail.Draw(NPC.oldPos, NPC.Size * 0.5f - Main.screenPosition - (NPC.rotation + (NPC.spriteDirection == -1 ? -PiOver2 : 0f)).ToRotationVector2() * NPC.scale * 16f, 40);
        }

        public float DashAfterimageWidthFunction(float completionRatio) => NPC.scale * Sqrt(1f - completionRatio) * 9f;

        public Color DashAfterimageColorFunction(float completionRatio)
        {
            Color c = Color.Lerp(Color.Teal, Color.GreenYellow, completionRatio);
            return c.HueShift(AfterimageTrailCompletion.Squared() * -0.4f) * NPC.Opacity * Sqrt(AfterimageTrailCompletion);
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
        }
    }
}
