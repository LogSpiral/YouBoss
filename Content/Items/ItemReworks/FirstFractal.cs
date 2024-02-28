using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.Items.SummonItems
{
    public class FirstFractal : ModItem
    {
        public static int UseTime => SecondsToFrames(0.58f);

        public static int BaseDamage => 256;

        public override string Texture => $"Terraria/Images/Item_{ItemID.FirstFractal}";

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 60;
            Item.damage = BaseDamage;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = UseTime;
            Item.useAnimation = UseTime;
            Item.useTurn = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.UseSound = null;
            Item.knockBack = 8f;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<FirstFractalHoldout>();
            Item.shootSpeed = 9f;
            Item.rare = ItemRarityID.Purple;
        }

        public override bool CanUseItem(Player player)
        {
            if (Item.type != ItemID.FirstFractal)
                return true;

            return player.ownedProjectileCounts[Item.shoot] <= 0;
        }
    }
}
