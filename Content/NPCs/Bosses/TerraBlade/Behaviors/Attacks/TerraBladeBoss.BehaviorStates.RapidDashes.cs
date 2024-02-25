using Microsoft.Xna.Framework;
using NoxusBoss.Assets;
using NoxusBoss.Common.Tools.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The amount of dashes that have been performed so far during the rapid dashes attack.
        /// </summary>
        public ref float RapidDashes_DashCounter => ref NPC.ai[0];

        /// <summary>
        /// How long the terra blade spins in place before reeling back during the rapid dashes attack.
        /// </summary>
        public int RapidDashes_SpinTime => SecondsToFrames(Lerp(0.3f, 0.195f, RapidDashes_DashCounter / RapidDashes_DashCount) + ByPhase(0.06f, 0f, -0.03f));

        /// <summary>
        /// How long the terra blade spends reeling back before dashing during the rapid dashes attack.
        /// </summary>
        public int RapidDashes_ReelBackTime => SecondsToFrames(Lerp(0.333f, 0.28f, RapidDashes_DashCounter / RapidDashes_DashCount) + ByPhase(0.04f, 0f, -0.04f));

        /// <summary>
        /// How long the terra blade spends dashing during the rapid dashes attack.
        /// </summary>
        public int RapidDashes_DashTime => SecondsToFrames(0.2f);

        /// <summary>
        /// The amount of dash sequences the terra blade does during the rapid dashes attack.
        /// </summary>
        public static int RapidDashes_DashCount => 4;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_RapidDashes()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.RapidDashes, null, false, () =>
            {
                return AITimer >= 42 && RapidDashes_DashCounter >= RapidDashes_DashCount;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.RapidDashes, DoBehavior_RapidDashes);
        }

        public float CalculateRapidDashesAttackWeight()
        {
            // Prevent the attack from happening after the telegraphed beam and radial spin attack, so that the player can get their bearings.
            // Testing revealed that it was unlikely the player would be prepared for a successive fast dash sequence after them.
            if (PreviousState == TerraBladeAIType.TelegraphedBeamDashes || PreviousState == TerraBladeAIType.DiamondSweeps)
                return 0f;

            // Prevent the attack from happening if the player is far away, as they won't be able to reasonably react in time.
            if (!NPC.WithinRange(Target.Center, 570f))
                return 0f;

            // Prioritize this attack a bit after projectile oriented attacks.
            if (PreviousState == TerraBladeAIType.DiamondSweeps || PreviousState == TerraBladeAIType.EnergyBeamSpin)
                return 1.6f;

            return 1f;
        }

        public void DoBehavior_RapidDashes()
        {
            int animationTime = RapidDashes_SpinTime + RapidDashes_ReelBackTime + RapidDashes_DashTime;
            bool doneAttacking = RapidDashes_DashCounter >= RapidDashes_DashCount;

            // Get near the target and look away from them in anticipation of the next attack if the attack is almost over.
            if (doneAttacking)
            {
                float flySpeedInterpolant = InverseLerp(0f, 42f, AITimer).Squared();
                NPC.SmoothFlyNear(Target.Center + new Vector2((Target.Center.X - NPC.Center.X).NonZeroSign() * -400f, -180f), flySpeedInterpolant * 0.4f, 1f - flySpeedInterpolant * 0.3f);
                NPC.velocity *= Lerp(0.9f, 1f, flySpeedInterpolant);

                // Look away from the target.
                NPC.rotation = NPC.AngleFrom(Target.Center) + 0.15f;

                // Make the shine and afterimages dissipate.
                AfterimageOpacity *= 0.5f;
                ShineInterpolant *= 0.8f;
                return;
            }

            // Spin in place.
            if (AITimer <= RapidDashes_SpinTime)
            {
                float spinSpeed = Convert01To010(AITimer / (float)RapidDashes_SpinTime * 0.72f).Squared() * 0.97f;
                NPC.rotation += spinSpeed;
                NPC.velocity *= 0.7f;
                AfterimageOpacity *= 0.85f;
                ShineInterpolant *= 0.8f;
            }

            // Reel back.
            else if (AITimer <= RapidDashes_SpinTime + RapidDashes_ReelBackTime)
            {
                float reelBackCompletion = InverseLerp(0f, RapidDashes_ReelBackTime, AITimer - RapidDashes_SpinTime);
                float reelBackSpeed = InverseLerp(0f, 0.6f, reelBackCompletion).Squared() * InverseLerp(1f, 0.867f, reelBackCompletion) * 26f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionToSafe(Target.Center) * -reelBackSpeed - Vector2.UnitY * 10f, 0.3f);
                NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(Target.Center), 0.2f);
            }

            // Perform the dash.
            else
            {
                if (AITimer == RapidDashes_SpinTime + RapidDashes_ReelBackTime + 1)
                {
                    // Play a dash sound.
                    SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound, NPC.Center);

                    // Reset afterimages.
                    NPC.oldPos = new Vector2[NPC.oldPos.Length];

                    StartShakeAtPoint(NPC.Center, 5f);
                    NPC.rotation = NPC.rotation.AngleTowards(NPC.AngleTo(Target.Center), 0.6f);
                    NPC.velocity = NPC.rotation.ToRotationVector2() * 120f;
                    NPC.netUpdate = true;
                }

                // Use dash visuals.
                ShineInterpolant = 1f;
                AfterimageOpacity = 1f;
                AfterimageClumpInterpolant = 0.7f;
                AfterimageTrailCompletion = InverseLerp(0f, RapidDashes_DashTime - 1, AITimer - RapidDashes_SpinTime - RapidDashes_ReelBackTime);

                // Slow down a bit if getting too far from the target.
                bool movingAwayFromTarget = Vector2.Dot(NPC.velocity, NPC.DirectionToSafe(Target.Center)) < 0f;
                if (!NPC.WithinRange(Target.Center, 900f) && movingAwayFromTarget)
                    NPC.velocity *= 0.86f;
            }

            // Perform the next dash.
            if (AITimer >= RapidDashes_SpinTime + RapidDashes_ReelBackTime + RapidDashes_DashTime)
            {
                AITimer = 0;
                RapidDashes_DashCounter++;
                NPC.netUpdate = true;
            }

            // Enable contact damage when moving fast.
            if (NPC.velocity.Length() >= 32f)
                NPC.damage = NPC.defDamage;
        }
    }
}
