using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace YouBoss.Core.Graphics.SpecificEffectManagers
{
    public class SilhouetteTargetContent : ARenderTargetContentByRequest
    {
        /// <summary>
        /// The host of this render target to draw.
        /// </summary>
        public Entity Host
        {
            get;
            internal set;
        }

        protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            // Initialize the underlying render target if necessary.
            Vector2 size = new(Main.screenWidth, Main.screenHeight);
            PrepareARenderTarget_WithoutListeningToEvents(ref _target, Main.instance.GraphicsDevice, (int)size.X, (int)size.Y, RenderTargetUsage.PreserveContents);

            device.SetRenderTarget(_target);
            device.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawPlayer();
            DrawTerraBlade();
            Main.spriteBatch.End();

            device.SetRenderTarget(null);

            // Mark preparations as completed.
            _wasPrepared = true;
        }

        private static void DrawPlayer()
        {
            int owner = Main.myPlayer;
            Player other = Main.player[owner];
            Player player = Main.playerVisualClone[owner] ??= new();

            player.CopyVisuals(other);
            player.isFirstFractalAfterImage = true;
            player.firstFractalAfterImageOpacity = 1f;
            player.ResetVisibleAccessories();
            player.UpdateDyes();
            player.DisplayDollUpdate();
            player.UpdateSocialShadow();
            player.itemRotation = 0f;
            player.heldProj = other.heldProj;
            player.Center = other.Center;
            player.wingFrame = other.wingFrame;
            player.velocity.Y = other.velocity.Y;
            player.PlayerFrame();
            player.socialIgnoreLight = true;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, player, player.position, 0f, player.fullRotationOrigin, 0f);
        }

        private void DrawTerraBlade()
        {
            if (!Host.active)
                return;

            if (Host is NPC n)
                Main.instance.DrawNPC(n.whoAmI, false);
            if (Host is Projectile p)
                Main.instance.DrawProj(p.whoAmI);
        }
    }
}
