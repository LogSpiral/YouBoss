using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public class SilhouetteTargetContent : ARenderTargetContentByRequest
    {
        /// <summary>
        /// The host of this render target to draw.
        /// </summary>
        public TerraBladeBoss Host
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

        private void DrawPlayer()
        {
            NPC npc = Host.NPC;
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
            player.itemAnimationMax = 0;
            player.itemAnimation = 0;
            player.itemRotation = 0f;
            player.heldProj = 0;
            player.Center = other.Center;
            player.itemRotation = 0f;
            player.wingFrame = other.wingFrame;
            player.velocity.Y = other.velocity.Y;
            player.PlayerFrame();
            player.socialIgnoreLight = true;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, npc.rotation);
            Main.PlayerRenderer.DrawPlayer(Main.Camera, player, player.position, 0f, player.fullRotationOrigin, 0f);
        }

        private static void DrawTerraBlade()
        {
            if (TerraBladeBoss.Myself is null)
                return;

            Main.instance.DrawNPC(TerraBladeBoss.Myself.whoAmI, false);
        }
    }
}
