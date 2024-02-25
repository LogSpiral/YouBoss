using Microsoft.Xna.Framework;
using YouBoss.Assets;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The max speed boost that beams should have during the accelerating beam wall attack.
        /// </summary>
        public float AcceleratingBeamWall_WallMaxSpeedBoost => ByPhase(12f, 15f, 22f);

        /// <summary>
        /// The acceleration boost that beams should have during the accelerating beam wall attack.
        /// </summary>
        public float AcceleratingBeamWall_WallAccelerationBoost => ByPhase(0.6f, 0.74f, 0.85f);

        /// <summary>
        /// How long the terra blade should spend redirecting during the the accelerating beam wall attack.
        /// </summary>
        public static int AcceleratingBeamWall_HoverRedirectTime => SecondsToFrames(0.5f);

        /// <summary>
        /// How long the terra blade should wait before slashing during the the accelerating beam wall attack.
        /// </summary>
        public static int AcceleratingBeamWall_SlashDelay => SecondsToFrames(0.16f);

        /// <summary>
        /// How long the terra blade should spend slashing during the the accelerating beam wall attack.
        /// </summary>
        public static int AcceleratingBeamWall_SlashTime => SecondsToFrames(0.18f);

        /// <summary>
        /// How long the terra blade should wait after the attack ends to transition the next one during the the accelerating beam wall attack.
        /// </summary>
        public static int AcceleratingBeamWall_AttackTransitionDelay => SecondsToFrames(1.2f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_AcceleratingBeamWall()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.AcceleratingBeamWall, null, false, () =>
            {
                return AITimer >= AcceleratingBeamWall_HoverRedirectTime + AcceleratingBeamWall_SlashDelay + AcceleratingBeamWall_SlashTime + AcceleratingBeamWall_AttackTransitionDelay;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.AcceleratingBeamWall, DoBehavior_AcceleratingBeamWall);
        }

        public float CalculateAcceleratingBeamWallAttackWeight()
        {
            return 1f;
        }

        public void DoBehavior_AcceleratingBeamWall()
        {
            // Redirect to the bottom left/right of the target at first.
            if (AITimer <= AcceleratingBeamWall_HoverRedirectTime)
            {
                // Store the forward direction.
                NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();

                // Perform hover movement.
                float hoverRedirectSpeed = InverseLerp(0f, AcceleratingBeamWall_HoverRedirectTime * 0.8f, AITimer).Cubed() * 0.9f;
                Vector2 hoverDestination = Target.Center + new Vector2(NPC.direction * -696f, 350f) + Target.Velocity * new Vector2(14f, 30f);
                NPC.SmoothFlyNear(hoverDestination, hoverRedirectSpeed, 0.32f);

                // Point the terra blade at the target.
                NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(Target.Center), 0.3f);

                return;
            }

            // Slow down after hovering.
            if (AITimer <= AcceleratingBeamWall_HoverRedirectTime + AcceleratingBeamWall_SlashDelay)
            {
                NPC.velocity *= 0.84f;

                // Point the blade forward.
                NPC.rotation = NPC.rotation.AngleLerp(0f, 0.33f);
                return;
            }

            // Perform the attack.
            if (AITimer <= AcceleratingBeamWall_HoverRedirectTime + AcceleratingBeamWall_SlashDelay + AcceleratingBeamWall_SlashTime)
            {
                PerformVFXForMultiplayer(() =>
                {
                    ParticleOrchestraSettings particleSettings = new()
                    {
                        PositionInWorld = NPC.Center + NPC.rotation.ToRotationVector2() * 104f,
                        MovementVector = (NPC.rotation + PiOver2).ToRotationVector2() * 7.5f + Main.rand.NextVector2Circular(4f, 4f)
                    };
                    ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.TerraBlade, particleSettings);
                });

                // Initialize velocity.
                if (AITimer == AcceleratingBeamWall_HoverRedirectTime + AcceleratingBeamWall_SlashDelay + 1)
                {
                    // Shake the screen.
                    StartShakeAtPoint(NPC.Center, 9f);

                    // Play sounds.
                    SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SlashSound);

                    NPC.velocity = Vector2.UnitX * NPC.direction * 70f;
                    NPC.netUpdate = true;
                }

                // Arc upwards.
                float spinArc = Pi / AcceleratingBeamWall_SlashTime * -0.75f;
                NPC.velocity = NPC.velocity.RotatedBy(spinArc * NPC.direction);
                NPC.rotation += spinArc;

                // Release the beams.
                Vector2 beamVelocity = Vector2.UnitX * NPC.direction * 12f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, beamVelocity, ModContent.ProjectileType<AcceleratingTerraBeam>(), TerraBeamDamage, 0f, -1, AcceleratingBeamWall_WallMaxSpeedBoost, AcceleratingBeamWall_WallAccelerationBoost);

                return;
            }

            if (AITimer <= AcceleratingBeamWall_HoverRedirectTime + AcceleratingBeamWall_SlashDelay + AcceleratingBeamWall_SlashTime + AcceleratingBeamWall_AttackTransitionDelay)
            {
                // Slow down.
                NPC.velocity *= 0.7f;

                // Point the terra blade away from the target.
                NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleFrom(Target.Center), 0.2f);
                return;
            }
        }
    }
}
