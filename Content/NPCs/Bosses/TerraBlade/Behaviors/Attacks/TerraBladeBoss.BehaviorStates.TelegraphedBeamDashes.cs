using Microsoft.Xna.Framework;
using YouBoss.Assets;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
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
        /// The amount of spacing between telegraphed beams during the telegraphed beam dashes attack.
        /// </summary>
        public float TelegraphedBeamDashes_BeamSpacing => ByPhase(204f, 195f, 185f);

        /// <summary>
        /// How long the terra blade spends dashing during the telegraphed beam dashes attack.
        /// </summary>
        public int TelegraphedBeamDashes_DashTime => SecondsToFrames(ByPhase(0.48f, 0.435f, 0.4f));

        /// <summary>
        /// The hover offset angle of the terra blade's telegraphed beam dashes attack.
        /// </summary>
        public ref float TelegraphedBeamDashes_HoverOffsetAngle => ref NPC.ai[0];

        /// <summary>
        /// The amount of dashes performed so far during the terra blade's telegraphed beam dashes attack.
        /// </summary>
        public ref float TelegraphedBeamDashes_DashCounter => ref NPC.ai[1];

        /// <summary>
        /// How long the terra blade spends hover redirecting during the telegraphed beam dashes attack.
        /// </summary>
        public int TelegraphedBeamDashes_HoverRedirectTime => SecondsToFrames(TelegraphedBeamDashes_DashCounter <= 0f ? 0.63f : 0.3167f);

        /// <summary>
        /// How long the terra blade spends reeling back during the telegraphed beam dashes attack.
        /// </summary>
        public static int TelegraphedBeamDashes_ReelBackTime => SecondsToFrames(0.45f);

        /// <summary>
        /// How long the terra blade waits before slowing the dash down during the telegraphed beam dashes attack.
        /// </summary>
        public static int TelegraphedBeamDashes_SlowdownDelay => SecondsToFrames(0.1833f);

        /// <summary>
        /// The amount of dashes the terra blade performs during the telegraphed beam dashes attack.
        /// </summary>
        public static int TelegraphedBeamDashes_DashCount => 3;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_TelegraphedBeamDashes()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.TelegraphedBeamDashes, null, false, () =>
            {
                return AITimer >= TelegraphedBeamDashes_HoverRedirectTime - 1 && TelegraphedBeamDashes_DashCounter >= TelegraphedBeamDashes_DashCount;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.TelegraphedBeamDashes, DoBehavior_TelegraphedBeamDashes);
        }

        public float CalculateTelegraphedBeamDashesAttackWeight()
        {
            return 1f;
        }

        public void DoBehavior_TelegraphedBeamDashes()
        {
            int beamTelegraphTime = 45;
            float dashSpeed = 105f;
            float hoverOffset = 372f;
            Vector2 hoverDestination = Target.Center - Vector2.UnitY.RotatedBy(TelegraphedBeamDashes_HoverOffsetAngle) * hoverOffset;

            // Hover above the target.
            if (AITimer <= TelegraphedBeamDashes_HoverRedirectTime)
            {
                // Make the shine and afterimage values dissipate.
                ShineInterpolant *= 0.8f;
                AfterimageOpacity *= 0.8f;

                // Initialize the hover offset angle.
                if (AITimer == 1)
                {
                    // The sword should pick the side which is between the empress and the player, and then randomly pick a place on the wall that forms from it.
                    TelegraphedBeamDashes_HoverOffsetAngle = Main.rand.NextFloatDirection() * PiOver2;
                    if (Vector2.UnitY.RotatedBy(TelegraphedBeamDashes_HoverOffsetAngle).X.NonZeroSign() != (Target.Center.X - NPC.Center.X).NonZeroSign())
                        TelegraphedBeamDashes_HoverOffsetAngle *= -1f;

                    NPC.velocity *= 0.1f;
                    NPC.netUpdate = true;
                }

                float hoverSpeedInterpolant = Lerp(0.01f, 0.35f, AITimer / (float)TelegraphedBeamDashes_HoverRedirectTime);

                NPC.SmoothFlyNear(hoverDestination, hoverSpeedInterpolant, 0.9f);
                NPC.rotation = NPC.rotation.AngleTowards(NPC.AngleTo(Target.Center), 0.9f);
                return;
            }

            // Reel back.
            if (AITimer < TelegraphedBeamDashes_HoverRedirectTime + TelegraphedBeamDashes_ReelBackTime)
            {
                float reelBackSpeed = InverseLerp(0f, TelegraphedBeamDashes_ReelBackTime, AITimer - TelegraphedBeamDashes_HoverRedirectTime) * 1.92f;
                if (AITimer <= TelegraphedBeamDashes_HoverRedirectTime + 3)
                    NPC.velocity *= 0.5f;

                NPC.velocity -= NPC.DirectionToSafe(Target.Center) * reelBackSpeed;
                NPC.velocity.Y -= reelBackSpeed * 0.8f;
                NPC.rotation = NPC.AngleTo(Target.Center);
            }

            // Dash at the target.
            if (AITimer == TelegraphedBeamDashes_HoverRedirectTime + TelegraphedBeamDashes_ReelBackTime)
            {
                // Play a dash sound.
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound, NPC.Center);

                // Shake the screen.
                StartShake(5f);

                // Perform the dash.
                Vector2 dashDirection = NPC.DirectionToSafe(Target.Center);
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
                NPC.rotation = dashDirection.ToRotation();
                NPC.velocity = dashDirection * dashSpeed;
                NPC.netUpdate = true;

                // Slice the screen.
                LinearScreenShoveSystem.CreateNew(24, NPC.Center, NPC.velocity.SafeNormalize(Vector2.UnitY), completionRatio =>
                {
                    return InverseLerp(0f, 0.15f, completionRatio) * InverseLerp(0.95f, 0.6f, completionRatio) * 18f;
                });

                // Release telegraphed beams.
                float beamSpeed = 25f;
                Vector2 perpendicularDashVelocity = (NPC.rotation + PiOver2).ToRotationVector2();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = -6; i <= 6; i++)
                    {
                        if (i == 0)
                            continue;

                        Vector2 beamSpawnPosition = NPC.Center - dashDirection * 576f + perpendicularDashVelocity * TelegraphedBeamDashes_BeamSpacing * i;
                        NewProjectileBetter(beamSpawnPosition, dashDirection * beamSpeed, ModContent.ProjectileType<TelegraphedTerraBeam>(), TerraBeamDamage, 0f, -1, beamTelegraphTime, (i == 1).ToInt());
                    }
                }
            }

            // Handle dash velocities.
            if (AITimer >= TelegraphedBeamDashes_HoverRedirectTime + TelegraphedBeamDashes_ReelBackTime)
            {
                // Deal contact damage.
                NPC.damage = NPC.defDamage;

                // Apply visual effects.
                ShineInterpolant = 1f;
                AfterimageOpacity = 1f;
                AfterimageClumpInterpolant = 0.4f;
                NPC.rotation = NPC.velocity.ToRotation();
                AfterimageTrailCompletion = InverseLerp(0f, TelegraphedBeamDashes_DashTime, AITimer - TelegraphedBeamDashes_HoverRedirectTime - TelegraphedBeamDashes_ReelBackTime);

                // Slow down after some time has passed, to ensure that the blade doesn't get super, super far away.
                if (AITimer >= TelegraphedBeamDashes_HoverRedirectTime + TelegraphedBeamDashes_ReelBackTime + TelegraphedBeamDashes_SlowdownDelay)
                    NPC.velocity *= 0.8f;
            }

            // Prepare for the next dash.
            if (AITimer >= TelegraphedBeamDashes_HoverRedirectTime + TelegraphedBeamDashes_ReelBackTime + TelegraphedBeamDashes_DashTime)
            {
                TelegraphedBeamDashes_DashCounter++;
                AITimer = 0;
                NPC.netUpdate = true;
            }
        }
    }
}
