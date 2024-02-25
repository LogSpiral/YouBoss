using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Assets
{
    public class MiscTexturesRegistry : ModSystem
    {
        #region Texture Path Constants

        public const string InvisiblePixelPath = $"{ExtraTexturesPath}/InvisiblePixel";

        public const string ExtraTexturesPath = "YouBoss/Assets/ExtraTextures";

        public const string GreyscaleTexturesPath = "YouBoss/Assets/ExtraTextures/GreyscaleTextures";

        public const string NoiseTexturesPath = "YouBoss/Assets/ExtraTextures/Noise";

        public const string ChromaticBurstPath = $"{GreyscaleTexturesPath}/ChromaticBurst";

        #endregion Texture Path Constants

        #region Greyscale Textures

        public static readonly Texture2D BloomCircleSmall = LoadDeferred($"{GreyscaleTexturesPath}/BloomCircleSmall");

        public static readonly Texture2D BloomFlare = LoadDeferred($"{GreyscaleTexturesPath}/BloomFlare");

        public static readonly Texture2D BloomLineTexture = LoadDeferred($"{GreyscaleTexturesPath}/BloomLine");

        public static readonly Texture2D ChromaticBurst = LoadDeferred(ChromaticBurstPath);

        public static readonly Texture2D ShineFlareTexture = LoadDeferred($"{GreyscaleTexturesPath}/ShineFlare");

        #endregion Greyscale Textures

        #region Noise Textures

        public static readonly Texture2D DendriticNoise = LoadDeferred($"{NoiseTexturesPath}/DendriticNoise");

        public static readonly Texture2D DendriticNoiseZoomedOut = LoadDeferred($"{NoiseTexturesPath}/DendriticNoiseZoomedOut");

        public static readonly Texture2D TurbulentNoise = LoadDeferred($"{NoiseTexturesPath}/TurbulentNoise");

        public static readonly Texture2D WavyBlotchNoise = LoadDeferred($"{NoiseTexturesPath}/WavyBlotchNoise");

        #endregion Noise Textures

        #region Invisible Pixel

        // Self-explanatory. Sometimes shaders need a "blank slate" in the form of an invisible texture to draw their true contents onto, which this can be beneficial for.
        public static readonly Texture2D InvisiblePixel = LoadDeferred(InvisiblePixelPath);

        #endregion Invisible Pixel

        #region Loader Utility

        private static Texture2D LoadDeferred(string path)
        {
            // Don't attempt to load anything server-side.
            if (Main.netMode == NetmodeID.Server)
                return default;

            return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        }

        #endregion Loader Utility
    }
}
