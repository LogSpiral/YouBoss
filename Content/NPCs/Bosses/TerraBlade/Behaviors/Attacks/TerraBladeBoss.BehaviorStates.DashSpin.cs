using Microsoft.Xna.Framework;
using YouBoss.Common.Tools.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// How long the terra blade's dash spin attack goes.
        /// </summary>
        public int DashSpin_AttackDuration => SecondsToFrames(ByPhase(3f, 2.8f, 2.56f));

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_DashSpin()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.DashSpin, null, false, () =>
            {
                return AITimer >= DashSpin_AttackDuration;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.DashSpin, DoBehavior_DashSpin);
        }

        public float CalculateDashSpinAttackWeight()
        {
            // Use a slightly higher default weight to ensure that this attack, representing a cooldown, happens frequently enough.
            return 1.3f;
        }

        public void DoBehavior_DashSpin()
        {
            // Initialize the spin velocity.
            if (AITimer == 1)
            {
                NPC.velocity = NPC.DirectionToSafe(Target.Center) * 7f;
                NPC.netUpdate = true;
            }

            // Deal damage after a short delay.
            if (AITimer >= 40)
                NPC.damage = NPC.defDamage;

            // Dash towards the target.
            float distanceToTarget = NPC.Distance(Target.Center);
            float dashSpeed = Remap(distanceToTarget, 375f, 185f, 41f, 16.5f) * InverseLerp(36f, 90f, distanceToTarget);
            NPC.Center = Vector2.Lerp(NPC.Center, Target.Center, 0.009f);
            NPC.velocity = Vector2.Clamp(NPC.velocity + NPC.DirectionToSafe(Target.Center) * 1.08f, -Vector2.One * dashSpeed, Vector2.One * dashSpeed);
            PlayerDrawOffsetFactor = InverseLerp(12f, 0f, AITimer);

            // Spin in place.
            NPC.rotation += TwoPi * NPC.velocity.X / 300f;
            ShineInterpolant = InverseLerp(0f, 15f, AITimer);
            AfterimageOpacity = ShineInterpolant;
            AfterimageClumpInterpolant = 0.5f;

            // Get repelled from the player if super close to them, to prevent telefrags.
            if (NPC.WithinRange(Target.Center, 32f))
            {
                NPC.velocity -= NPC.DirectionToSafe(Target.Center) * 32f;
                NPC.netUpdate = true;
            }
        }
    }
}
