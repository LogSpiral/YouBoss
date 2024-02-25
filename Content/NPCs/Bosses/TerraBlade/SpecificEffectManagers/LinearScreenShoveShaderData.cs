using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Graphics.Effects;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers;

namespace YouBoss.Core.Graphics.Shaders.Screen
{
    public class LinearScreenShoveShaderData : ScreenShaderData
    {
        public const string ShaderKey = "YouBoss:LinearScreenShoveShader";

        public LinearScreenShoveShaderData(string passName)
            : base(passName)
        {
        }

        public LinearScreenShoveShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            bool shouldBeActive = LinearScreenShoveSystem.ShoveLines.Any(s => s?.Active ?? false);
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
            {
                Filters.Scene[ShaderKey].Opacity = 1f;
                Filters.Scene.Activate(ShaderKey);
            }
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
            {
                Filters.Scene[ShaderKey].Opacity = 0f;
                Filters.Scene.Deactivate(ShaderKey);
            }
        }

        public override void Apply()
        {
            float[] lineWidths = new float[LinearScreenShoveSystem.MaxLines];
            Vector4[] lineData = new Vector4[LinearScreenShoveSystem.MaxLines];
            ScreenShoveLine[] shoveLines = LinearScreenShoveSystem.ShoveLines;

            for (int i = 0; i < LinearScreenShoveSystem.MaxLines; i++)
            {
                ScreenShoveLine line = shoveLines[i];
                if (line is null)
                    continue;

                float width = line.WidthFunction(line.Time / (float)line.Lifetime);
                if (width <= 0.001f)
                    continue;

                Vector2 lineDirection = (line.Direction * new Vector2(Main.screenHeight / (float)Main.screenWidth, 1f)).RotatedBy(PiOver2);
                Vector2 origin = WorldSpaceToScreenUV(line.Origin);

                lineWidths[i] = width / MathF.Max(Main.screenWidth, Main.screenHeight);
                lineData[i] = new(lineDirection.X, lineDirection.Y, origin.X, origin.Y);
            }

            Shader.Parameters["lines"]?.SetValue(lineData);
            Shader.Parameters["lineWidths"]?.SetValue(lineWidths);

            base.Apply();
        }
    }
}
