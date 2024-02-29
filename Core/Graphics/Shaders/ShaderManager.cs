using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Core.Graphics.Shaders.Screen;

namespace YouBoss.Core.Graphics.Shaders
{
    public class ShaderManager : ModSystem
    {
        private static Dictionary<string, ManagedShader> shaders;

        public static bool HasFinishedLoading
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            // Don't attempt to load shaders on servers.
            if (Main.netMode == NetmodeID.Server)
                return;

            shaders = [];
            foreach (var path in Mod.GetFileNames().Where(f => f.Contains("Assets/Effects/")))
            {
                // Ignore paths inside of the compiler directory.
                if (path.Contains("Compiler/"))
                    continue;

                string shaderName = Path.GetFileNameWithoutExtension(path);
                string clearedPath = Path.Combine(Path.GetDirectoryName(path), shaderName).Replace(@"\", @"/");
                Ref<Effect> shader = new(Mod.Assets.Request<Effect>(clearedPath, AssetRequestMode.ImmediateLoad).Value);
                SetShader(shaderName, shader);
            }

            // This is kind of hideous but I'm not sure how to best handle these screen shaders. Perhaps some marker in the file name or a dedicated folder?
            Ref<Effect> s1 = new(Mod.Assets.Request<Effect>("Assets/Effects/ScreenDistortions/LinearScreenShoveShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[LinearScreenShoveShaderData.ShaderKey] = new Filter(new LinearScreenShoveShaderData(s1, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s2 = new(Mod.Assets.Request<Effect>("Assets/Effects/ScreenDistortions/RadialScreenShoveShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[RadialScreenShoveShaderData.ShaderKey] = new Filter(new RadialScreenShoveShaderData(s2, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            Ref<Effect> s3 = new(Mod.Assets.Request<Effect>("Assets/Effects/ScreenDistortions/LocalScreenDistortionShader", AssetRequestMode.ImmediateLoad).Value);
            Filters.Scene[ScreenDistortShaderData.ShaderKey] = new Filter(new ScreenDistortShaderData(s2, ManagedShader.DefaultPassName), EffectPriority.VeryHigh);

            HasFinishedLoading = true;
        }

        public static ManagedShader GetShader(string name) => shaders[name];

        public static void SetShader(string name, Ref<Effect> newShaderData) => shaders[name] = new(name, newShaderData);

        public override void PostUpdateEverything()
        {
            RadialScreenShoveShaderData.ToggleActivityIfNecessary();
            LinearScreenShoveShaderData.ToggleActivityIfNecessary();
            ScreenDistortShaderData.ToggleActivityIfNecessary();
        }
    }
}
