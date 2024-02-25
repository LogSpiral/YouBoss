using Microsoft.Xna.Framework;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The standard spin angular velocity during the energy beam spin attack.
        /// </summary>
        public float EnergyBeamSpin_StandardSpinAngularVelocity => ToRadians(ByPhase(29f, 25.32f, 23f));

        /// <summary>
        /// How long the terra blade waits before creating beams during its beam spin attack.
        /// </summary>
        public int EnergyBeamSpin_ProjectileReleaseDelay => SecondsToFrames(ByPhase(1f, 0.9f, 0.75f));

        /// <summary>
        /// How long the terra blade spends creating beams during its beam spin attack.
        /// </summary>
        public int EnergyBeamSpin_ProjectileReleaseTime => SecondsToFrames(ByPhase(1.4f, 1.4f, 1.5f));

        /// <summary>
        /// How long the terra blade waits before transitioning to the next attack during its beam spin attack.
        /// </summary>
        public static int EnergyBeamSpin_AttackTransitionDelay => SecondsToFrames(1.5f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_EnergyBeamSpin()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.EnergyBeamSpin, null, false, () =>
            {
                return AITimer >= EnergyBeamSpin_ProjectileReleaseDelay + EnergyBeamSpin_ProjectileReleaseTime + EnergyBeamSpin_AttackTransitionDelay;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.EnergyBeamSpin, DoBehavior_EnergyBeamSpin);
        }

        public float CalculateEnergyBeamSpinAttackWeight()
        {
            // Make this attack not happen if the previous attack was the radial sweeps, to allow melee-specific attacks to shine after that instead.
            if (PreviousState == TerraBladeAIType.DiamondSweeps)
                return 0f;

            return 1f;
        }

        public void DoBehavior_EnergyBeamSpin()
        {
            // Attempt to approach the player.
            float defaultFlySpeed = 7f;
            float superFastMovementInterpolant = Pow(InverseLerp(30f, 0f, AITimer), 0.75f);
            Vector2 superFastVelocity = (Target.Center - NPC.Center) * InverseLerp(200f, 320f, NPC.Distance(Target.Center)) * 0.15f;
            Vector2 defaultVelocity = NPC.DirectionToSafe(Target.Center) * defaultFlySpeed;
            Vector2 idealVelocity = Vector2.Lerp(defaultVelocity, superFastVelocity, superFastMovementInterpolant);
            if (!NPC.WithinRange(Target.Center, 150f))
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.15f);
            else
                NPC.velocity *= 0.93f;

            // Initialize the spin direction.
            if (AITimer == 1)
            {
                NPC.direction = -(Target.Center.X - NPC.Center.X).NonZeroSign();
                NPC.netUpdate = true;
            }

            // Spin around.
            int attackEndTimer = AITimer - EnergyBeamSpin_ProjectileReleaseDelay - EnergyBeamSpin_ProjectileReleaseTime;
            float spinAngularVelocity = InverseLerp(EnergyBeamSpin_AttackTransitionDelay, 0f, attackEndTimer).Squared() * EnergyBeamSpin_StandardSpinAngularVelocity;
            NPC.rotation += NPC.direction * spinAngularVelocity;
            PlayerDrawOffsetFactor = InverseLerp(12f, 0f, AITimer);

            // Release arcing beams.
            bool canReleaseProjectiles = AITimer >= EnergyBeamSpin_ProjectileReleaseDelay && AITimer <= EnergyBeamSpin_ProjectileReleaseDelay + EnergyBeamSpin_ProjectileReleaseTime;
            if (canReleaseProjectiles)
            {
                if (AITimer % 5 == 0)
                    SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { MaxInstances = 8 }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, NPC.rotation.ToRotationVector2() * 4f, ModContent.ProjectileType<ArcingTerraBeam>(), TerraBeamDamage, 0f, -1, NPC.direction * spinAngularVelocity * 0.02f);
            }
        }
    }
}
