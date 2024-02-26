using Microsoft.Xna.Framework;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The anticipation angle of the terra blade.
        /// </summary>
        public float AnticipationAngle
        {
            get
            {
                int previousSpriteDirection = NPC.spriteDirection;
                float anticipationAngle = -3.6f;
                if (Target.Center.X < NPC.Center.X)
                {
                    anticipationAngle = (anticipationAngle.ToRotationVector2() * new Vector2(-1f, 1f)).ToRotation() + PiOver2;
                    NPC.spriteDirection = -1;
                }
                else
                    NPC.spriteDirection = 1;

                // Update the rotation immediately if a previous sprite direction occurred, to ensure that there's no strange angle redirecting.
                if (NPC.spriteDirection != previousSpriteDirection)
                    NPC.rotation -= NPC.spriteDirection * PiOver2;

                return anticipationAngle;
            }
        }

        /// <summary>
        /// How long the terra blade should aim at the target.
        /// </summary>
        public static int ThreateninglyAimAtTarget_AimTime => SecondsToFrames(1.8f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_ThreateninglyAimAtTarget()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.ThreateninglyAimAtTarget, TerraBladeAIType.SingleSwipe, false, () =>
            {
                return AITimer >= ThreateninglyAimAtTarget_AimTime;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.ThreateninglyAimAtTarget, DoBehavior_ThreateninglyAimAtTarget);
        }

        public void DoBehavior_ThreateninglyAimAtTarget()
        {
            PerformingStartAnimation = true;

            // Disable damage.
            NPC.dontTakeDamage = true;

            // Slow down.
            NPC.velocity *= 0.91f;

            // Make the player materialize.
            PlayerAppearanceInterpolant = Saturate(PlayerAppearanceInterpolant + 0.033f);

            // Shine.
            float pointAtTargetInterpolant = InverseLerp(0f, 10f, AITimer).Squared();
            ShineInterpolant = Clamp(ShineInterpolant + pointAtTargetInterpolant * 0.029f, 0f, 1f);

            if (AITimer == 32)
                SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal, NPC.Center);

            // Keep the camera on the blade.
            CameraPanSystem.CameraPanInterpolant = InverseLerp(1f, 0.91f, ShineInterpolant);
            CameraPanSystem.CameraFocusPoint = NPC.Center;

            // Look at the target before swiping in anticipation.
            float idealAngle = NPC.AngleTo(Target.Center).AngleLerp(NPC.AngleFrom(Target.Center) + 0.05f, ShineInterpolant * 0.6f + 0.4f);
            NPC.rotation = NPC.rotation.AngleLerp(idealAngle, pointAtTargetInterpolant * 0.07f).AngleTowards(idealAngle, pointAtTargetInterpolant * 0.09f);

            // Reel back.
            NPC.velocity -= NPC.DirectionToSafe(Target.Center) * pointAtTargetInterpolant * Sqrt(ShineInterpolant) * 0.18f;
            NPC.velocity.Y -= ShineInterpolant * InverseLerp(1f, 0.75f, ShineInterpolant) * 0.2f;
        }
    }
}
