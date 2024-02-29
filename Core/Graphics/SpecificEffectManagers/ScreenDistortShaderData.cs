using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using YouBoss.Core.Graphics.Automators;
using YouBoss.Core.Graphics.SpecificEffectManagers;

namespace YouBoss.Core.Graphics.Shaders.Screen
{
    public class ScreenDistortShaderData(Ref<Effect> shader, string passName) : ScreenShaderData(shader, passName)
    {
        public const string ShaderKey = "YouBoss:LocalScreenDistortion";

        /// <summary>
        /// The render target that holds all distortion render data.
        /// </summary>
        public static LocalDistortionTargetContent DistortionDrawContents
        {
            get;
            private set;
        }

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            bool shouldBeActive = Main.projectile.Any(p =>
            {
                return p.active && p.ModProjectile is IDrawLocalDistortion drawer && !p.IsOffscreen();
            });
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
            {
                Filters.Scene[ShaderKey].Opacity = 1f;
                Filters.Scene.Activate(ShaderKey);
            }
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
            {
                Filters.Scene[ShaderKey].Opacity = 0f;
                Filters.Scene.Deactivate(ShaderKey);
            }
        }

        public override void Apply()
        {
            // Initialize the distortion drawer.
            if (DistortionDrawContents is null)
            {
                DistortionDrawContents = new();
                Main.ContentThatNeedsRenderTargets.Add(DistortionDrawContents);
            }

            // If the drawer isn't ready, wait until it is.
            DistortionDrawContents.Request();

            // Supply the distortion target to the graphics device.
            var gd = Main.instance.GraphicsDevice;
            gd.Textures[1] = DistortionDrawContents.IsReady ? DistortionDrawContents.GetTarget() : InvisiblePixel;
            gd.SamplerStates[1] = SamplerState.LinearClamp;
            base.Apply();
        }
    }
}
