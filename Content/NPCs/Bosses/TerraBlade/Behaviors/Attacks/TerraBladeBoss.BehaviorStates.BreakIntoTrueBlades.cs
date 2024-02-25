using System.Linq;
using Microsoft.Xna.Framework;
using YouBoss.Assets;
using YouBoss.Common.Tools.DataStructures;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles;
using YouBoss.Content.Particles;
using YouBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Content.Particles;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The AI timer during the true blades attack relative to how long it's been since the spin concluded.
        /// </summary>
        public int PostSpinTimer => AITimer - BreakIntoTrueBlades_SplitSlowdownTime - BreakIntoTrueBlades_SplitMoveWaitTime - BreakIntoTrueBlades_SplitSpinTime - BreakIntoTrueBlades_SplitSpinSlowdownTime;

        /// <summary>
        /// How long it takes for split blades to slow down after being released during the true blades attack.
        /// </summary>
        public int BreakIntoTrueBlades_SplitSlowdownTime => SecondsToFrames(InPhase3 ? 0.25f : 0.333f);

        /// <summary>
        /// How long it takes for split blades to go back to moving again after completely slowing down during the true blades attack.
        /// </summary>
        public int BreakIntoTrueBlades_SplitMoveWaitTime => SecondsToFrames(InPhase3 ? 0.3f : 0.333f);

        /// <summary>
        /// How long the split blades spin in place uninterrupted during the true blades attack.
        /// </summary>
        public int BreakIntoTrueBlades_SplitSpinTime => SecondsToFrames(InPhase3 ? 0.45f : 0.583f);

        /// <summary>
        /// The spin offset during of the split blades during the true blades attack.
        /// </summary>
        public ref float BreakIntoTrueBlades_BladeOffsetDirection => ref NPC.ai[0];

        /// <summary>
        /// How long the split blades slow down their spin during the true blades attack.
        /// </summary>
        public static int BreakIntoTrueBlades_SplitSpinSlowdownTime => SecondsToFrames(0.55f);

        /// <summary>
        /// How long the split blades spend rising during the true blades attack.
        /// </summary>
        public static int BreakIntoTrueBlades_SplitRiseTime => SecondsToFrames(0.133f);

        /// <summary>
        /// How long the split blades wait before stabbing during the true blades attack.
        /// </summary>
        public static int BreakIntoTrueBlades_SplitStabDelay => SecondsToFrames(0.533f);

        /// <summary>
        /// How long the split blades spend stabbing during the true blades attack.
        /// </summary>
        public static int BreakIntoTrueBlades_SplitStabTime => SecondsToFrames(0.0667f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_BreakIntoTrueBlades()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.BreakIntoTrueBlades, null, false, () =>
            {
                return PostSpinTimer >= BreakIntoTrueBlades_SplitStabDelay + BreakIntoTrueBlades_SplitStabTime + 142;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.BreakIntoTrueBlades, DoBehavior_BreakIntoTrueBlades);
        }

        public float CalculateBreakIntoTrueBladesAttackWeight()
        {
            return InPhase3 ? 1.2f : 1f;
        }

        public void DoBehavior_BreakIntoTrueBlades()
        {
            int splitBladeID = ModContent.ProjectileType<TerraBladeSplit>();
            Vector2 bladeOffsetDirection = -Vector2.UnitY.RotatedBy(BreakIntoTrueBlades_BladeOffsetDirection);
            if (AITimer == 1)
            {
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SplitSound, NPC.Center);

                // Create the three blades.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 bladeVelocity = bladeOffsetDirection * 50f;
                    NewProjectileBetter(NPC.Center, bladeVelocity, splitBladeID, 0, 0f, -1, (int)TerraBladeSplit.BladeVariant.BrokenHeroSword);
                    NewProjectileBetter(NPC.Center, bladeVelocity.RotatedBy(TwoPi * 0.333f), splitBladeID, 0, 0f, -1, (int)TerraBladeSplit.BladeVariant.TrueNightsEdge);
                    NewProjectileBetter(NPC.Center, bladeVelocity.RotatedBy(TwoPi * 0.667f), splitBladeID, 0, 0f, -1, (int)TerraBladeSplit.BladeVariant.TrueExcalibur);
                }

                // Shake the screen.
                StartShake(11f);

                // Create visual effects.
                PerformVFXForMultiplayer(() =>
                {
                    RadialScreenShoveSystem.Start(NPC.Center, 60);

                    // Create special particles.
                    ExpandingChromaticBurstParticle burst = new(NPC.Center, Vector2.Zero, Color.Teal, 16, 0.1f);
                    burst.Spawn();
                    BloomParticle bloom = new(NPC.Center, new(206, 255, 150), 2f, 90);
                    bloom.Spawn();
                    bloom = new(NPC.Center, Color.White, 1f, 60);
                    bloom.Spawn();
                });

                // Initialize the spin direction.
                BreakIntoTrueBlades_BladeOffsetDirection = Main.rand.NextFloat(TwoPi);

                // Disappear.
                NPC.Opacity = 0f;
                NPC.netUpdate = true;
                return;
            }

            // Calculate timers.
            float moveToPlayerInterpolant = InverseLerp(0f, 9f, AITimer - BreakIntoTrueBlades_SplitSlowdownTime - BreakIntoTrueBlades_SplitMoveWaitTime).Squared();
            float spinInterpolant = InverseLerp(BreakIntoTrueBlades_SplitSpinSlowdownTime, 0f, AITimer - BreakIntoTrueBlades_SplitSlowdownTime - BreakIntoTrueBlades_SplitMoveWaitTime - BreakIntoTrueBlades_SplitSpinTime).Cubed();

            float riseInterpolant = InverseLerp(0f, BreakIntoTrueBlades_SplitRiseTime, PostSpinTimer);
            float stabInterpolant = InverseLerp(0f, BreakIntoTrueBlades_SplitStabTime, PostSpinTimer - BreakIntoTrueBlades_SplitStabDelay);

            // Find all blades.
            var blades = AllProjectilesByID(splitBladeID).ToList();
            if (blades.Count < 3)
                return;

            // Disable damage and visibility if necessary.
            if (NPC.Opacity <= 0f)
            {
                NPC.dontTakeDamage = true;
                NPC.ShowNameOnHover = false;
            }

            // Find all individual blade projectiles.
            Projectile heroSword = blades.FirstOrDefault(b => b.ai[0] == (int)TerraBladeSplit.BladeVariant.BrokenHeroSword);
            Projectile nightsEdge = blades.FirstOrDefault(b => b.ai[0] == (int)TerraBladeSplit.BladeVariant.TrueNightsEdge);
            Projectile excalibur = blades.FirstOrDefault(b => b.ai[0] == (int)TerraBladeSplit.BladeVariant.TrueExcalibur);
            if (heroSword is null || nightsEdge is null || excalibur is null)
                return;

            // Puppeteer the various blades as necessary.
            if (AITimer < BreakIntoTrueBlades_SplitSlowdownTime)
            {
                heroSword.velocity *= 0.75f;
                nightsEdge.velocity *= 0.75f;
                excalibur.velocity *= 0.75f;
            }
            else
            {
                // Play a dash sound prior to the blades redirecting.
                if (AITimer == BreakIntoTrueBlades_SplitSlowdownTime + BreakIntoTrueBlades_SplitMoveWaitTime + 3)
                    SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound, NPC.Center);

                // Play a sound prior to the blades rising.
                if (PostSpinTimer == 3)
                    SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal, NPC.Center);

                BreakIntoTrueBlades_BladeOffsetDirection += TwoPi * spinInterpolant / 42f;

                NPC.SmoothFlyNear(Target.Center, moveToPlayerInterpolant * (1f - riseInterpolant) * 0.6f, 0.5f);

                float bladeFlySpeed = moveToPlayerInterpolant * 0.34f;
                Vector2 hoverOffset = bladeOffsetDirection * 320f;
                Vector2 heroSwordRiseOffset = heroSword.DirectionToSafe(NPC.Center) * riseInterpolant * -180f - Vector2.UnitY * riseInterpolant * 100f;
                Vector2 nightsEdgeRiseOffset = nightsEdge.DirectionToSafe(NPC.Center) * riseInterpolant * -180f - Vector2.UnitY * riseInterpolant * 100f;
                Vector2 excaliburRiseOffset = excalibur.DirectionToSafe(NPC.Center) * riseInterpolant * -180f - Vector2.UnitY * riseInterpolant * 100f;
                heroSword.SmoothFlyNear(NPC.Center + (hoverOffset + heroSwordRiseOffset) * (1f - stabInterpolant), bladeFlySpeed, 0.5f);
                nightsEdge.SmoothFlyNear(NPC.Center + (hoverOffset.RotatedBy(TwoPi * 0.333f) + nightsEdgeRiseOffset) * (1f - stabInterpolant), bladeFlySpeed, 0.5f);
                excalibur.SmoothFlyNear(NPC.Center + (hoverOffset.RotatedBy(TwoPi * 0.667f) + excaliburRiseOffset) * (1f - stabInterpolant), bladeFlySpeed, 0.5f);
            }

            // Make the blades do damage when stabbing.
            if (stabInterpolant > 0f)
            {
                int bladeDamage = NPC.defDamage;
                if (Main.expertMode)
                    bladeDamage /= 4;
                else
                    bladeDamage /= 2;

                heroSword.damage = bladeDamage;
                nightsEdge.damage = bladeDamage;
                excalibur.damage = bladeDamage;
            }

            if (PostSpinTimer == BreakIntoTrueBlades_SplitStabDelay)
                StartShake(16f);
            if (PostSpinTimer == BreakIntoTrueBlades_SplitStabDelay + BreakIntoTrueBlades_SplitStabTime + 8)
            {
                // Kill the blades.
                IProjOwnedByBoss<TerraBladeBoss>.KillAll();

                // Shake the screen.
                StartShake(15f);
                RadialScreenShoveSystem.Start(NPC.Center, 90);

                // Create a lens flare effect at the impact site.
                LensFlareParticle flare = new(NPC.Center, Color.YellowGreen, 60, 0.8f);
                flare.Spawn();
                flare = new(NPC.Center, Color.Wheat, 60, 0.6f);
                flare.Spawn();

                // Release sparks of metal.
                PerformVFXForMultiplayer(() =>
                {
                    for (int i = 0; i < 75; i++)
                    {
                        float metalLength = Main.rand.NextFloat(0.75f, 1f);
                        Color metalColor = Color.Lerp(Color.Yellow, Color.Teal, Main.rand.NextFloat());
                        metalColor = Color.Lerp(metalColor, Color.Silver, 0.6f);
                        Vector2 metalVelocity = Main.rand.NextVector2Circular(30f, 25f) - Vector2.UnitY * 10f;
                        MetalSparkParticle metal = new(NPC.Center, metalVelocity, metalVelocity.Length() <= 11f, Main.rand.Next(12, 19), new Vector2(0.285f, metalLength) * 0.4f, 1f, metalColor, Color.Silver);
                        metal.Spawn();
                    }
                });

                // Play an impact sound.
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.BladeReformExplosionSound, NPC.Center);

                // Release spreads of light and night beams.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int beamCount = 20;
                    float bladeArcSpeed = ToRadians(0.51f);
                    for (int i = 0; i < beamCount; i++)
                    {
                        Vector2 lightBladeVelocity = (TwoPi * i / beamCount).ToRotationVector2() * 3f;
                        Vector2 nightBladeVelocity = (TwoPi * (i + 0.5f) / beamCount).ToRotationVector2() * 1.25f;
                        NewProjectileBetter(NPC.Center, lightBladeVelocity, ModContent.ProjectileType<ArcingLightBeam>(), TerraBeamDamage, 0f, -1, -bladeArcSpeed);
                        NewProjectileBetter(NPC.Center, nightBladeVelocity, ModContent.ProjectileType<ArcingNightBeam>(), TerraBeamDamage, 0f, -1, bladeArcSpeed);
                        NewProjectileBetter(NPC.Center, lightBladeVelocity * 0.2f, ModContent.ProjectileType<ArcingLightBeam>(), TerraBeamDamage, 0f, -1, bladeArcSpeed);
                        if (InPhase3)
                            NewProjectileBetter(NPC.Center, lightBladeVelocity * 0.2f, ModContent.ProjectileType<ArcingNightBeam>(), TerraBeamDamage, 0f, -1, -bladeArcSpeed, -5f);
                    }

                    NPC.Opacity = 1f;
                    NPC.netUpdate = true;
                }
            }

            // Look away from the player after reforming.
            if (stabInterpolant >= 1f && PostSpinTimer <= BreakIntoTrueBlades_SplitStabDelay + BreakIntoTrueBlades_SplitStabTime + 34)
                NPC.rotation = NPC.AngleTo(Target.Center) + Pi;

            if (stabInterpolant < 1f)
            {
                heroSword.rotation = heroSword.AngleTo(NPC.Center) + PiOver4;
                nightsEdge.rotation = nightsEdge.AngleTo(NPC.Center) + PiOver4;
                excalibur.rotation = excalibur.AngleTo(NPC.Center) + PiOver4;
            }
        }
    }
}
