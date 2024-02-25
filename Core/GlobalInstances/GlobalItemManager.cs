using Terraria;
using Terraria.ModLoader;

namespace YouBoss.Core.Graphics.Shaders.Screen
{
    public class GlobalItemManager : GlobalItem
    {
        public delegate void WingUpdateDelegate(Item item, Player player, ref float horizontalSpeed, ref float horizontalAcceleration, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend);

        /// <summary>
        /// The event responsible for wing updates.
        /// </summary>
        public static event WingUpdateDelegate WingUpdateEvent;

        public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
            float _ = 0f;
            WingUpdateEvent?.Invoke(item, player, ref speed, ref acceleration, ref _, ref _, ref _, ref _, ref _);
        }

        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            float _ = 0f;
            WingUpdateEvent?.Invoke(item, player, ref _, ref _, ref ascentWhenFalling, ref ascentWhenRising, ref maxCanAscendMultiplier, ref maxAscentMultiplier, ref constantAscend);
        }
    }
}
