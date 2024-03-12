using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using YouBoss.Content.Items.ItemReworks;
using YouBoss.Core;
using YouBoss.Core.CrossCompatibility.Inbound;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC, IBossChecklistSupport
    {
        public bool IsMiniboss => false;

        public string ChecklistEntryName => "Yourself";

        public float ProgressionValue => 19.75f;

        public bool IsDefeated => WorldSaveSystem.HasDefeatedYourself;

        public List<int> Collectibles => [ModContent.ItemType<FirstFractal>()];

        public bool UsesCustomPortraitDrawing => true;

        public void DrawCustomPortrait(SpriteBatch spriteBatch, Rectangle area, Color color)
        {
            // Initialize the player drawer, with the terra blade as its current host.
            PlayerDrawContents.Host = this;
            PlayerDrawContents.Request();

            // If the drawer isn't ready, wait until it is.
            if (!PlayerDrawContents.IsReady)
                return;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Texture2D bossChecklistTexture = PlayerDrawContents.GetTarget();
            Vector2 centeredDrawPosition = area.Center.ToVector2() - bossChecklistTexture.Size() * 0.5f;
            spriteBatch.Draw(bossChecklistTexture, centeredDrawPosition, color);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }
    }
}
