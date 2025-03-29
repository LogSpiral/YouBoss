using Terraria.Graphics.Effects;
using Terraria;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers
{
    public class TerraBladeSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => TerraBladeBoss.Myself?.ModNPC is TerraBladeBoss terraBlade && !terraBlade.PerformingStartAnimation;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            string skyKey = TerraBladeSky.SkyKey;
            if (SkyManager.Instance[skyKey] is not null)
            {
                if (isActive)
                    SkyManager.Instance.Activate(skyKey);
                else
                    SkyManager.Instance.Deactivate(skyKey);
            }
        }
    }
}
