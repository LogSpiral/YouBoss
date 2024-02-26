using System;
using Microsoft.Xna.Framework;
using YouBoss.Assets;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Common.Utilities;
using YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers;
using YouBoss.Content.Particles;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The vertical deceleration factor in the aerial swoop dashes attack.
        /// </summary>
        public ref float AerialSwoopDashes_VerticalDecelerationFactor => ref NPC.ai[0];

        /// <summary>
        /// The amount of elapsed dashes in the aerial swoop dashes attack.
        /// </summary>
        public ref float AerialSwoopDashes_DashCounter => ref NPC.ai[1];

        /// <summary>
        /// How long the terra blade spends redirecting in the aerial swoop dashes attack.
        /// </summary>
        public static int AerialSwoopDashes_HoverRedirectTime => SecondsToFrames(0.2167f);

        /// <summary>
        /// How long the terra blade waits before dashing in the aerial swoop dashes attack.
        /// </summary>
        public int AerialSwoopDashes_DashWaitTime => (int)(24 - AerialSwoopDashes_DashCounter * 5f) + (AerialSwoopDashes_DashCounter == 0f).ToInt() * 61;

        /// <summary>
        /// How long dashes last during the aerial swoop dashes attack.
        /// </summary>
        public static int AerialSwoopDashes_DashTime => SecondsToFrames(0.2167f);

        /// <summary>
        /// How long the terra blade waits before transitioning to the next attack during the aerial swoop dashes attack.
        /// </summary>
        public static int AerialSwoopDashes_AttackTransitionDelay => SecondsToFrames(0.75f);

        /// <summary>
        /// The amount of dashes the perform during the aerial swoop dashes attack.
        /// </summary>
        public static int AerialSwoopDashes_DashCount => 5;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_AerialSwoopDashes()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.AerialSwoopDashes, null, false, () =>
            {
                return AerialSwoopDashes_DashCounter >= AerialSwoopDashes_DashCount && AITimer >= AerialSwoopDashes_AttackTransitionDelay;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.AerialSwoopDashes, DoBehavior_AerialSwoopDashes);
        }

        public float CalculateAerialSwoopDashesAttackWeight()
        {
            // Prioritize this attack a bit since it's in the last phase.
            return 1.5f;
        }

        public void DoBehavior_AerialSwoopDashes()
        {
            int hoverRedirectTime = 12;
            int dashWaitTime = (int)(24 - AerialSwoopDashes_DashCounter * 4f);
            float swoopSpeed = 62f;
            float horizontalSpeed = 110f;
            float verticalSwoopHeight = 320f;
            bool doneAttacking = AerialSwoopDashes_DashCounter >= AerialSwoopDashes_DashCount;

            if (doneAttacking)
            {
                NPC.velocity *= 0.85f;
                NPC.spriteDirection = 1;
                NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(Target.Center), 0.25f);
                return;
            }

            // Hover into position for the dash.
            if (AITimer <= hoverRedirectTime)
            {
                float hoverRedirectSpeed = InverseLerp(0f, hoverRedirectTime / 2, AITimer).Squared() * 0.4f;
                Vector2 hoverOffset = new((Target.Center.X - NPC.Center.X).NonZeroSign() * -560f, -verticalSwoopHeight);
                NPC.SmoothFlyNear(Target.Center + hoverOffset, hoverRedirectSpeed, 0.4f);

                // Look at the target.
                NPC.spriteDirection = (Target.Center.X - NPC.Center.X).NonZeroSign();

                // Look away from the player in anticipation of the slash.
                float aimAwaySpeedInterpolant = InverseLerp(0f, 7f, hoverRedirectTime);
                float idealRotation = NPC.AngleFrom(Target.Center);
                if (NPC.spriteDirection == -1)
                    idealRotation += PiOver2;
                NPC.rotation = NPC.rotation.AngleLerp(idealRotation, aimAwaySpeedInterpolant * 0.5f);

                // Reset the afterimage trail.
                AfterimageTrailCompletion = 0f;
            }

            // Slow down.
            else if (AITimer <= hoverRedirectTime + dashWaitTime)
            {
                // Shake the screen a bit and play a sound before the hover redirect before the first swoop as a telegraph.
                if (AITimer == hoverRedirectTime + 1 && AerialSwoopDashes_DashCounter == 0f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFurySwing with { Volume = 1.6f });
                    StartShake(7.4f, shakeStrengthDissipationIncrement: 0.33f);
                    PulseRingParticle ring = new(NPC.Center, Color.Teal, 2.3f, 0f, 32);
                    ring.Spawn();

                    NPC.velocity *= 0.15f;
                    NPC.netUpdate = true;
                }

                NPC.velocity *= 0.85f;
                NPC.rotation += TwoPi * NPC.spriteDirection / dashWaitTime;
            }

            // Handle post-dash behaviors.
            else
            {
                // Use the afterimage trail.
                AfterimageTrailCompletion = 0.9f;

                // Look forward.
                float idealRotation = NPC.velocity.ToRotation();
                if (NPC.spriteDirection == -1)
                    idealRotation += PiOver2;
                NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.36f);

                // Accelerate forward, decelerate downward to give an arc motion.
                NPC.velocity.X = Clamp(NPC.velocity.X + NPC.velocity.X.NonZeroSign() * horizontalSpeed * 0.25f, -horizontalSpeed, horizontalSpeed);
                NPC.velocity.Y *= AerialSwoopDashes_VerticalDecelerationFactor;

                // Release perpendicular beams.
                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.velocity.Length() >= horizontalSpeed - 20f && AITimer % 3 == 0)
                {
                    float maxBeamSpeedBoost = 10.7f;
                    Vector2 perpendicularVelocity = NPC.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2) * 1.5f;
                    NewProjectileBetter(NPC.Center, -perpendicularVelocity, ModContent.ProjectileType<AcceleratingTerraBeam>(), TerraBeamDamage, 0f, -1, maxBeamSpeedBoost);
                    NewProjectileBetter(NPC.Center, perpendicularVelocity, ModContent.ProjectileType<AcceleratingTerraBeam>(), TerraBeamDamage, 0f, -1, maxBeamSpeedBoost);
                }

                // Enable damage.
                NPC.damage = NPC.defDamage;
            }

            // Perform the dash.
            if (AITimer == hoverRedirectTime + dashWaitTime)
            {
                float startingHorizontalTargetDistance = Distance(NPC.Center.X, Target.Center.X);

                // Calculate the deceleration factor using a bit of calculus.
                // https://cdn.discordapp.com/attachments/1129281417663238178/1211092772052803605/screenshot.png?ex=65ecf109&is=65da7c09&hm=d91841b5db5f159db73417cfb4a505bab20eb9cc7be3a289ff39cd0f4ad8135e&
                float timeUntilTargetIsReached = Round(startingHorizontalTargetDistance / horizontalSpeed) + 3f;
                double calculateSwoopDistance(double decelerationFactor)
                {
                    return (Math.Pow(decelerationFactor, timeUntilTargetIsReached) - 1f) / Math.Log(decelerationFactor) - verticalSwoopHeight / swoopSpeed;
                }
                AerialSwoopDashes_VerticalDecelerationFactor = (float)Utilities.IterativelySearchForRoot(calculateSwoopDistance, 0.8f, 15);

                // Update the velocity and old position cache.
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
                NPC.velocity.X = NPC.HorizontalDirectionTo(Target.Center).NonZeroSign() * horizontalSpeed * 0.2f;
                NPC.velocity.Y = swoopSpeed;
                NPC.netUpdate = true;

                // Play dash sounds.
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SlashSound);

                // Shake the screen.
                StartShake(8f);
            }

            // Slice the screen momentarily after dashing.
            if (AITimer == hoverRedirectTime + dashWaitTime + 8)
            {
                // Slice the screen.
                PerformVFXForMultiplayer(() =>
                {
                    LinearScreenShoveSystem.CreateNew(15, NPC.Center, (NPC.velocity * new Vector2(1f, 0.6f)).SafeNormalize(Vector2.Zero), completionRatio =>
                    {
                        return InverseLerp(0f, 0.05f, completionRatio) * InverseLerp(0.95f, 0.6f, completionRatio) * 16f;
                    });
                });
            }

            // Proceed to the next dash.
            if (AITimer >= hoverRedirectTime + dashWaitTime + AerialSwoopDashes_DashTime)
            {
                NPC.velocity *= 0.2f;
                AerialSwoopDashes_DashCounter++;
                AITimer = 0;
                NPC.netUpdate = true;

                // Kill residual projectiles when the attack ends.
                if (AerialSwoopDashes_DashCounter >= AerialSwoopDashes_DashCount)
                {
                    foreach (Projectile beam in AllProjectilesByID(ModContent.ProjectileType<AcceleratingTerraBeam>()))
                    {
                        beam.timeLeft = 12;
                        beam.netUpdate = true;
                    }
                }
            }
        }
    }
}
