using System;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class LinearScreenShoveSystem : ModSystem
    {
        /// <summary>
        /// The maximum amount of lines that can exist at once. This is corresponds with the shader's internal array sizes.
        /// </summary>
        public const int MaxLines = 10;

        /// <summary>
        /// The set of all shove lines. Not all are guaranteed to be active.
        /// </summary>
        public static readonly ScreenShoveLine[] ShoveLines = new ScreenShoveLine[MaxLines];

        public static void CreateNew(int lifetime, Vector2 origin, Vector2 lineDirection, Func<float, float> widthFunction)
        {
            for (int i = 0; i < ShoveLines.Length; i++)
            {
                ScreenShoveLine line = ShoveLines[i];
                if (line?.Active ?? false)
                    continue;

                ShoveLines[i] = new(lifetime, lineDirection, origin, widthFunction);
                break;
            }
        }

        public override void PostUpdateEverything()
        {
            // Update shove lines.
            for (int i = 0; i < MaxLines; i++)
                ShoveLines[i]?.Update();
        }
    }
}
