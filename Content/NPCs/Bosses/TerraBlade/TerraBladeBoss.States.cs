using System.Linq;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Common.Tools.StateMachines;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// Backing field for the state machine.
        /// </summary>
        private PushdownAutomata<BossAIState<TerraBladeAIType>, TerraBladeAIType> stateMachine;

        /// <summary>
        /// The blade's state machine. This is responsible for all handling of its AI, such as behavior management, AI timers, etc.
        /// </summary>
        public PushdownAutomata<BossAIState<TerraBladeAIType>, TerraBladeAIType> StateMachine
        {
            get
            {
                if (stateMachine is null)
                    LoadStateMachine();
                return stateMachine;
            }
            private set => stateMachine = value;
        }

        public void PerformStateSafetyCheck()
        {
            // Add the relevant phase cycle if it has been exhausted.
            if ((StateMachine?.StateStack?.Count ?? 1) <= 0 || !StateMachine.StateStack.Any())
                StateMachine.StateStack.Push(StateMachine.StateRegistry[TerraBladeAIType.ResetCycle]);
        }

        /// <summary>
        /// Loads the blade's state machine, registering transition requirements and actions pertaining to it.
        /// </summary>
        public void LoadStateMachine()
        {
            // Initialize the AI state machine.
            StateMachine = new(new(TerraBladeAIType.AppearanceAnimation));
            StateMachine.OnStateTransition += ResetGenericVariables;

            // Register all states in the machine.
            for (int i = 0; i < (int)TerraBladeAIType.Count; i++)
                StateMachine.RegisterState(new((TerraBladeAIType)i));

            // Load state transitions.
            AutomatedMethodInvokeAttribute.InvokeWithAttribute(this);
        }

        private void ResetGenericVariables(bool stateWasPopped)
        {
            if (stateWasPopped)
            {
                // Reset the NPC.ai values.
                NPC.ai[0] = 0f;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;

                // Reset the sprite direction.
                NPC.spriteDirection = 1;
            }
            if (UpcomingAttacks.Any())
                UpcomingAttacks.RemoveAt(0);

            NPC.netUpdate = true;
        }
    }
}
