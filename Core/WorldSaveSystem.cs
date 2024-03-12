using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace YouBoss.Core
{
    public class WorldSaveSystem : ModSystem
    {
        /// <summary>
        /// Whether the player boss has been defeated.
        /// </summary>
        public static bool HasDefeatedYourself
        {
            get;
            set;
        }

        public override void OnWorldLoad() => HasDefeatedYourself = false;

        public override void OnWorldUnload() => HasDefeatedYourself = false;

        public override void SaveWorldData(TagCompound tag)
        {
            if (HasDefeatedYourself)
                tag["HasDefeatedYourself"] = true;
        }

        public override void LoadWorldData(TagCompound tag) => HasDefeatedYourself = tag.ContainsKey("HasDefeatedYourself");

        public override void NetSend(BinaryWriter writer)
        {
            BitsByte b1 = new();
            b1[0] = HasDefeatedYourself;

            writer.Write(b1);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte b1 = reader.ReadByte();
            HasDefeatedYourself = b1[0];
        }
    }
}
