using YouBoss.Common.Tools.DataStructures;
using YouBoss.Common.Tools.Reflection;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers;
using YouBoss.Core.Graphics.SpecificEffectManagers;
using YouBoss.Common.Tools.Easings;
using Terraria;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ID;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
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

        public ref float EnterPhase3_EyeGleamInterpolant => ref NPC.ai[0];

        public static int EnterPhase3_FlyInFrontOfPlayerTime => SecondsToFrames(0.3f);

        public static int EnterPhase3_SilhouetteShadowAppearTime => SecondsToFrames(0.15f);

        public static int EnterPhase3_AimSwordTime => SecondsToFrames(0.3f);

        public static int EnterPhase3_EyeGleamTime => SecondsToFrames(1.9f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_EnterPhase3()
        {
            // Prepare to enter phase 2 if ready. This will ensure that once the attack has finished the terra blade will enter the second phase.
            StateMachine.AddTransitionStateHijack(originalState =>
            {
                if (!InPhase3 && LifeRatio < Phase3LifeRatio)
                    return TerraBladeAIType.EnterPhase3;

                return originalState;
            }, _ =>
            {
                InPhase3 = true;
                PreviousTwoStates[^1] = FirstPhase3Attack;
            });
            StateMachine.RegisterTransition(TerraBladeAIType.EnterPhase3, FirstPhase3Attack, false, () =>
            {
                return AITimer >= EnterPhase3_FlyInFrontOfPlayerTime + EnterPhase3_SilhouetteShadowAppearTime + EnterPhase3_EyeGleamTime + EnterPhase3_AimSwordTime;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.EnterPhase3, DoBehavior_EnterPhase3);
        }

        public void DoBehavior_EnterPhase3()
        {
            // Kill all lingering projectiles at first.
            if (AITimer < 5)
                IProjOwnedByBoss<TerraBladeBoss>.KillAll();

            // Disable damage.
            NPC.dontTakeDamage = true;

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                AITimer = 0;
                EnterPhase3_EyeGleamInterpolant = 0f;
            }

            // Fly in front of the player.
            if (AITimer <= EnterPhase3_FlyInFrontOfPlayerTime)
            {
                NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();
                Vector2 hoverDestination = Target.Center + new Vector2(NPC.direction * -400f, -4f);
                NPC.SmoothFlyNear(hoverDestination, 0.5f, 0.5f);

                // Aim the terra blade at the target.
                NPC.spriteDirection = 1;
                NPC.rotation = NPC.AngleTo(Target.Center) + NPC.direction * 0.4f;
                TerraBladeSilhouetteDrawSystem.SilhouetteOpacity = 0f;

                return;
            }

            // Slow down and move the camera onto the terra blade.
            NPC.velocity *= 0.8f;
            bool aimingSword = AITimer >= EnterPhase3_FlyInFrontOfPlayerTime + EnterPhase3_SilhouetteShadowAppearTime + EnterPhase3_EyeGleamTime;
            float silhouetteAppearInterpolant = InverseLerp(0f, EnterPhase3_SilhouetteShadowAppearTime, AITimer - EnterPhase3_FlyInFrontOfPlayerTime);

            if (!aimingSword)
            {
                TerraBladeSilhouetteDrawSystem.SilhouetteOpacity = silhouetteAppearInterpolant;
                CameraPanSystem.CameraFocusPoint = NPC.Center;
                CameraPanSystem.CameraPanInterpolant = PolynomialEasing.Cubic.Evaluate(EasingType.InOut, silhouetteAppearInterpolant);
            }

            // Raise the sword.
            if (silhouetteAppearInterpolant >= 1f && !aimingSword)
                NPC.rotation = NPC.rotation.AngleLerp(-PiOver2, 0.11f);

            // Make the eye gleam.
            bool eyeCanGleam = silhouetteAppearInterpolant >= 1f && Distance(NPC.rotation, -PiOver2) <= 0.02f && !aimingSword;
            if (eyeCanGleam)
            {
                if (EnterPhase3_EyeGleamInterpolant <= 0f)
                {
                    SoundEngine.PlaySound(TwinkleSound);
                    StartShake(5f);
                }
                EnterPhase3_EyeGleamInterpolant = Saturate(EnterPhase3_EyeGleamInterpolant + 0.03f);
            }

            if (aimingSword)
            {
                EnterPhase3_EyeGleamInterpolant = Saturate(EnterPhase3_EyeGleamInterpolant * 0.8f - 0.05f);
                NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(Target.Center), 0.3f);
                NPC.velocity -= NPC.rotation.ToRotationVector2() * 2f;

                TerraBladeSilhouetteDrawSystem.SilhouetteOpacity = Saturate(TerraBladeSilhouetteDrawSystem.SilhouetteOpacity - 0.3f);
            }
        }

        public void DrawEyeGleam()
        {
            float eyePulse = Main.GlobalTimeWrappedHourly * 2f % 1f;
            Texture2D eyeGleam = TextureAssets.Extra[98].Value;
            Vector2 eyePosition = NPC.Center + new Vector2(NPC.direction * 4f, 10f);
            Vector2 horizontalGleamScaleSmall = new Vector2(EnterPhase3_EyeGleamInterpolant * 3f, 1f) * 0.36f;
            Vector2 verticalGleamScaleSmall = new Vector2(1f, EnterPhase3_EyeGleamInterpolant * 2f) * 0.36f;
            Vector2 horizontalGleamScaleBig = horizontalGleamScaleSmall * (1f + eyePulse * 2f);
            Vector2 verticalGleamScaleBig = verticalGleamScaleSmall * (1f + eyePulse * 2f);
            Color eyeGleamColorSmall = new Color(255, 0, 94, 32) * EnterPhase3_EyeGleamInterpolant;
            Color eyeGleamColorBig = eyeGleamColorSmall * (1f - eyePulse);

            // Draw a pulsating red eye.
            Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleSmall, 0, 0f);
            Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorSmall, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleSmall, 0, 0f);
            Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, horizontalGleamScaleBig, 0, 0f);
            Main.spriteBatch.Draw(eyeGleam, eyePosition - Main.screenPosition, null, eyeGleamColorBig, 0f, eyeGleam.Size() * 0.5f, verticalGleamScaleBig, 0, 0f);
        }
    }
}
