using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace YouBoss.Core.Graphics.Shaders.Screen
{
    public class GlobalItemManager : GlobalItem
    {
        public delegate void ItemAction(Item item);

        public delegate bool ItemPlayerCondition(Item item, Player player);

        public delegate void WingUpdateDelegate(Item item, Player player, ref float horizontalSpeed, ref float horizontalAcceleration, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend);

        /// <summary>
        /// The event responsible for item set default behaviors.
        /// </summary>
        public static event ItemAction SetDefaultsEvent;

        /// <summary>
        /// The event responsible for usability condition behaviors.
        /// </summary>
        public static event ItemPlayerCondition CanUseItemEvent;

        /// <summary>
        /// The event responsible for wing updates.
        /// </summary>
        public static event WingUpdateDelegate WingUpdateEvent;

        public override void SetDefaults(Item item)
        {
            SetDefaultsEvent?.Invoke(item);
        }

        public override bool CanUseItem(Item item, Player player)
        {
            bool result = base.CanUseItem(item, player);
            if (CanUseItemEvent is null)
                return result;

            foreach (ItemPlayerCondition d in CanUseItemEvent?.GetInvocationList()?.Cast<ItemPlayerCondition>())
                result &= d.Invoke(item, player);

            return result;
        }

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
