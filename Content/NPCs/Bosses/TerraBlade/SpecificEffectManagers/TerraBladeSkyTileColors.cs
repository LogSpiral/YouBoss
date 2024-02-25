using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers
{
    public class TerraBladeSkyTileColors : ModSystem
    {
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            backgroundColor = Color.Lerp(backgroundColor, new(200, 200, 255), TerraBladeSky.Opacity * 0.13f);
            tileColor = Color.Lerp(tileColor, new(175, 205, 255), TerraBladeSky.Opacity * 0.8f);
        }
    }
}
