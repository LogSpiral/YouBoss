using Terraria.Graphics.Effects;
using Terraria;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers;
public class TerraBladeSkySceneResetPlayer : ModPlayer 
{
    //If exit when the boss still exists
    //Return the world will find the sky scene still actived
    //So we need to set the instance to Null Manually on enter the world.
    public override void OnEnterWorld() => TerraBladeBoss.Myself = null;
}
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
