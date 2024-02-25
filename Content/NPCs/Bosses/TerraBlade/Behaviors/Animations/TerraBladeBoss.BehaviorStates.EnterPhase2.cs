using Microsoft.Xna.Framework;
using YouBoss.Common.Tools.DataStructures;
using YouBoss.Common.Tools.Reflection;
using Terraria;
using Terraria.ModLoader;
using YouBoss.Core.Graphics.SpecificEffectManagers;
using YouBoss.Content.Particles;
using Terraria.Audio;
using YouBoss.Assets;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// Whether the terra blade is in its second phase.
        /// </summary>
        public bool Phase2
        {
            get;
            set;
        }

        /// <summary>
        /// The first attack that should be performed upon entering phase 2.
        /// </summary>
        public static TerraBladeAIType FirstPhase2Attack => TerraBladeAIType.DiamondSweeps;

        /// <summary>
        /// The life ratio at which the terra blade enters its second phase.
        /// </summary>
        public const float Phase2LifeRatio = 0.6f;

        /// <summary>
        /// How long the terra blade spends redirecting in its second phase transition.
        /// </summary>
        public static int EnterPhase2_HoverRedirectTime => SecondsToFrames(0.3f);

        /// <summary>
        /// How long the terra blade waits after redirecting in its second phase transition to create flash visuals.
        /// </summary>
        public static int EnterPhase2_FlashDelay => SecondsToFrames(0.6f);

        /// <summary>
        /// How long the terra blade performs back-image animations during its second phase transition.
        /// </summary>
        public static int EnterPhase2_BackImageAnimationTime => SecondsToFrames(0.4f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_EnterPhase2()
        {
            // Prepare to enter phase 2 if ready. This will ensure that once the attack has finished the terra blade will enter the second phase.
            StateMachine.AddTransitionStateHijack(originalState =>
            {
                if (!Phase2 && LifeRatio < Phase2LifeRatio)
                    return TerraBladeAIType.EnterPhase2;

                return originalState;
            }, _ =>
            {
                PreviousTwoStates[^1] = FirstPhase2Attack;
            });
            StateMachine.RegisterTransition(TerraBladeAIType.EnterPhase2, FirstPhase2Attack, false, () =>
            {
                return AITimer >= EnterPhase2_HoverRedirectTime + EnterPhase2_FlashDelay + EnterPhase2_BackImageAnimationTime + 20;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.EnterPhase2, DoBehavior_EnterPhase2);
        }

        public void DoBehavior_EnterPhase2()
        {
            // Kill all lingering projectiles at first.
            if (AITimer < 4)
                IProjOwnedByBoss<TerraBladeBoss>.KillAll();

            // Register the terra blade as being in the second phase.
            if (!Phase2)
            {
                Phase2 = true;
                NPC.netUpdate = true;
            }

            // Hover to the side of the player at first.
            if (AITimer < EnterPhase2_HoverRedirectTime)
            {
                // Look upward.
                NPC.spriteDirection = (Target.Center.X - NPC.Center.X).NonZeroSign();
                float idealRotation = -PiOver2;
                if (NPC.spriteDirection == -1)
                    idealRotation = 0f;

                Vector2 hoverDestination = Target.Center + new Vector2(NPC.spriteDirection * -450f, -200f);
                NPC.SmoothFlyNear(hoverDestination, 0.56f, 0.3f);
                NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.3f).AngleTowards(idealRotation, 0.03f);
                return;
            }

            // Slow down to a halt.
            NPC.velocity *= 0.51f;

            // Disable damage.
            NPC.dontTakeDamage = true;

            // Move the camera onto the terra blade.
            CameraPanSystem.CameraPanInterpolant = InverseLerp(4f, 10f, AITimer - EnterPhase2_HoverRedirectTime);
            CameraPanSystem.CameraFocusPoint = NPC.Center;

            // Create a blur and flash effect.
            if (AITimer == EnterPhase2_HoverRedirectTime + EnterPhase2_FlashDelay)
            {
                // Create an explosion burst.
                PerformVFXForMultiplayer(() =>
                {
                    ExpandingChromaticBurstParticle burst = new(NPC.Center, Vector2.Zero, Color.Lime, 16, 0.15f);
                    burst.Spawn();
                });

                // Play sounds.
                SoundEngine.PlaySound(TwinkleSound);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound);

                StartShake(9f);
                RadialScreenShoveSystem.Start(NPC.Center, 120);
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
            }

            // Create back-image effects.
            float backImageAnimationInterpolant = InverseLerp(0f, EnterPhase2_BackImageAnimationTime, AITimer - EnterPhase2_HoverRedirectTime - EnterPhase2_FlashDelay);
            BackImageScale = backImageAnimationInterpolant * 2.5f + NPC.scale;
            BackImageOpacity = 1f - backImageAnimationInterpolant;
            NPC.rotation -= NPC.spriteDirection * InverseLerpBump(0f, 0.15f, 0.3f, 1f, backImageAnimationInterpolant) * 0.06f;
        }
    }
}
