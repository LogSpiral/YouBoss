using NoxusBoss.Common.Tools.DataStructures;
using NoxusBoss.Common.Tools.Reflection;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// Whether the terra blade is in its second phase.
        /// </summary>
        public bool InPhase3
        {
            get;
            set;
        }

        /// <summary>
        /// The first attack that should be performed upon entering phase 2.
        /// </summary>
        public static TerraBladeAIType FirstPhase3Attack => TerraBladeAIType.BreakIntoTrueBlades;

        /// <summary>
        /// The life ratio at which the terra blade enters its third phase.
        /// </summary>
        public const float Phase3LifeRatio = 0.3f;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_EnterPhase3()
        {
            // Prepare to enter phase 2 if ready. This will ensure that once the attack has finished Nameless will enter the second phase.
            StateMachine.AddTransitionStateHijack(originalState =>
            {
                if (!InPhase3 && LifeRatio < Phase3LifeRatio)
                    return TerraBladeAIType.EnterPhase3;

                return originalState;
            }, _ =>
            {
                PreviousTwoStates[^1] = FirstPhase3Attack;
            });
            StateMachine.RegisterTransition(TerraBladeAIType.EnterPhase3, FirstPhase3Attack, false, () =>
            {
                return AITimer >= 2;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.EnterPhase3, DoBehavior_EnterPhase3);
        }

        public void DoBehavior_EnterPhase3()
        {
            // Kill all lingering projectiles at first.
            IProjOwnedByBoss<TerraBladeBoss>.KillAll();
            InPhase3 = true;
        }
    }
}
