using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Content.NPCs.Bosses.TerraBlade;

namespace YouBoss.Content.Items.SummonItems
{
    public class CursedMirror : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Purple;
            Item.value = 0;
        }

        public override bool CanUseItem(Player player) =>
            !NPC.AnyNPCs(ModContent.NPCType<TerraBladeBoss>());

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int bladeID = ModContent.NPCType<TerraBladeBoss>();

                // If the player is not in multiplayer, spawn the terra blade.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(player.whoAmI, bladeID);

                // If the player is in multiplayer, request a boss spawn.
                else
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: bladeID);
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddTile(TileID.LunarCraftingStation).
                AddIngredient(ItemID.Glass, 50).
                AddIngredient(ItemID.LunarBar, 8).
                Register();
        }
    }
}
