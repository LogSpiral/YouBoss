using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using YouBoss.Core.Graphics.Shaders;
using YouBoss.Content.NPCs.Bosses.TerraBlade;

namespace YouBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class TerraBladeSilhouetteDrawSystem : ModSystem
    {
        /// <summary>
        /// The silhouette target responsible.
        /// </summary>
        public static SilhouetteTargetContent SilhouetteDrawContents
        {
            get;
            private set;
        }

        /// <summary>
        /// The opacity of the silhouette effect.
        /// </summary>
        public static float SilhouetteOpacity
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the silhouette effect should be inverted in terms of contrast choices or not.
        /// </summary>
        public static bool Inverted
        {
            get;
            set;
        }

        /// <summary>
        /// The entity that should be included in the silhouette.
        /// </summary>
        public static Entity Subject
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            SilhouetteDrawContents = new();
            Main.ContentThatNeedsRenderTargets.Add(SilhouetteDrawContents);
            Main.OnPostDraw += DrawSilhouette;
        }

        private void DrawSilhouette(GameTime obj)
        {
            if (Main.gameMenu)
            {
                SilhouetteOpacity = 0f;
                Subject = null;
            }

            // Don't waste resources if the silhouette is not in use.
            if (SilhouetteOpacity <= 0f || Subject is null || !Subject.active)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Initialize the silhouette drawer, with the terra blade as its current host.
            SilhouetteDrawContents.Host = Subject;
            SilhouetteDrawContents.Request();

            // If the drawer isn't ready, wait until it is.
            if (!SilhouetteDrawContents.IsReady)
            {
                Main.spriteBatch.End();
                return;
            }

            // Draw the black background.
            Main.spriteBatch.Draw(Pixel, Vector2.Zero, null, (Inverted ? Color.White : Color.Black) * SilhouetteOpacity, 0f, Vector2.Zero, new Vector2(Main.screenWidth, Main.screenHeight), 0, 0f);

            // Draw the silhouette as pure white.
            ManagedShader silhouetteShader = ShaderManager.GetShader("SilhouetteShader");
            silhouetteShader.TrySetParameter("inverted", Inverted);
            silhouetteShader.Apply();
            Main.spriteBatch.Draw(SilhouetteDrawContents.GetTarget(), Vector2.Zero, null, Color.White * SilhouetteOpacity, 0f, Vector2.Zero, 1f, 0, 0f);

            // Draw the eye gleam over everything, resetting the silhouette shader.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (Subject is NPC n && n.ModNPC is TerraBladeBoss blade)
                blade.DrawEyeGleam();
            Main.spriteBatch.End();
        }
    }
}
