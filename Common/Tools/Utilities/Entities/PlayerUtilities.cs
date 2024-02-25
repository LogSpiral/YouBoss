﻿using System.Reflection;
using NoxusBoss.Common.Tools.DataStructures;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        private static FieldInfo adrenalineField;

        private static FieldInfo rageField;

        /// <summary>
        /// Gives a given <see cref="Player"/> the Boss Effects buff from Calamity, if it's enabled. This buff provides a variety of common effects, such as the near complete removal of natural enemy spawns.
        /// </summary>
        /// <param name="p">The player to apply the buff to.</param>
        public static void GrantBossEffectsBuff(this Player p)
        {
            if (ModReferences.BaseCalamity is null)
                return;

            if (!ModReferences.BaseCalamity.TryFind("BossEffects", out ModBuff bossEffects))
                return;

            p.AddBuff(bossEffects.Type, 2);
        }

        /// <summary>
        /// Gives a given <see cref="Player"/> infinite flight in accordance with Calamity's system, if it's enabled.
        /// </summary>
        /// <param name="p">The player to apply infinite flight to.</param>
        public static void GrantInfiniteFlight(this Player p)
        {
            ModReferences.BaseCalamity?.Call("ToggleInfiniteFlight", p, true);
        }

        /// <summary>
        /// Resets rage and adrenaline for a given <see cref="Player"/>.
        /// </summary>
        /// <param name="p">The player to reset ripper values for.</param>
        public static void ResetRippers(this Player p)
        {
            foreach (ModPlayer modPlayer in p.ModPlayers)
            {
                if (modPlayer.Name != "CalamityPlayer")
                    continue;

                // Initialize field information if necessary.
                adrenalineField ??= modPlayer.GetType().GetField("adrenaline");
                rageField ??= modPlayer.GetType().GetField("rage");

                adrenalineField?.SetValue(modPlayer, 0f);
                rageField?.SetValue(modPlayer, 0f);
            }
        }

        /// <summary>
        /// Gets the current mouse item for a given <see cref="Player"/>. This supports <see cref="Main.mouseItem"/> (the item held by the cursor) and <see cref="Player.HeldItem"/> (the item in use with the hotbar).
        /// </summary>
        /// <param name="player">The player to retrieve the mouse item for.</param>
        public static Item HeldMouseItem(this Player player)
        {
            if (!Main.mouseItem.IsAir)
                return Main.mouseItem;

            return player.HeldItem;
        }

        public static Referenced<T> GetValueRef<T>(this Player player, string key) =>
            player.GetModPlayer<PlayerDataManager>().valueRegistry.GetValueRef<T>(key);

        public static Referenced<T> GetValueRef<T>(this PlayerDataManager player, string key) =>
            player.valueRegistry.GetValueRef<T>(key);
    }
}
