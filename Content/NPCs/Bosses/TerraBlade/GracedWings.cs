using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Core.Graphics.Shaders.Screen;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public class GracedWings : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            GlobalItemManager.WingUpdateEvent += ApplyWingBuffs;
        }

        public static void ApplyWingBuffs(Item item, Player player, ref float horizontalSpeed, ref float horizontalAcceleration, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (!player.HasBuff<GracedWings>())
                return;

            horizontalSpeed = MathF.Max(horizontalSpeed, 14.1f);
            horizontalAcceleration = MathF.Max(horizontalAcceleration, 0.7f);
            ascentWhenFalling = MathF.Max(ascentWhenFalling, 0.85f);
            ascentWhenRising = MathF.Max(ascentWhenRising, 0.15f);
            maxCanAscendMultiplier = MathF.Max(ascentWhenRising, 1.1f);
            maxAscentMultiplier = MathF.Max(maxAscentMultiplier, 2.7f);
            constantAscend = MathF.Max(constantAscend, 0.135f);
        }
    }
}
