using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.ModLoader;

namespace YouBoss.Core.CrossCompatibility.Inbound
{
    public class BossChecklistCompatibilitySystem : ModSystem
    {
        internal static Mod BossChecklist;

        public override void PostSetupContent()
        {
            // Don't load anything if boss checklist is not enabled.
            if (!ModLoader.TryGetMod("BossChecklist", out BossChecklist))
                return;

            // Collect all NPCs that should adhere to boss checklist.
            var modNPCsWithBossChecklistSupport = Mod.LoadInterfacesFromContent<ModNPC, IBossChecklistSupport>();

            // Load boss checklist information via mod calls.
            foreach (var modNPC in modNPCsWithBossChecklistSupport)
            {
                IBossChecklistSupport checklistInfo = modNPC as IBossChecklistSupport;
                string registerCall = checklistInfo.IsMiniboss ? "LogMiniBoss" : "LogBoss";

                Dictionary<string, object> extraInfo = new()
                {
                    ["collectibles"] = checklistInfo.Collectibles
                };
                if (checklistInfo.SpawnItem is not null)
                    extraInfo["spawnItems"] = checklistInfo.SpawnItem.Value;
                if (checklistInfo.UsesCustomPortraitDrawing)
                    extraInfo["customPortrait"] = new Action<SpriteBatch, Rectangle, Color>(checklistInfo.DrawCustomPortrait);
                extraInfo["despawnMessage"] = Language.GetText($"Mods.{Mod.Name}.NPCs.{modNPC.Name}.BossChecklistIntegration.DespawnMessage");

                // Use the mod call.
                string result = (string)BossChecklist.Call(
                [
                    registerCall,
                    Mod,
                    checklistInfo.ChecklistEntryName,
                    checklistInfo.ProgressionValue,
                    () => checklistInfo.IsDefeated,
                    modNPC.Type,
                    extraInfo
                ]);
            }
        }
    }
}
