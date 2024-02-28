using YouBoss.Common.Tools.Reflection;
using Terraria;
using Terraria.ModLoader;
using YouBoss.Content.Particles;
using Microsoft.Xna.Framework;
using YouBoss.Core.Graphics.SpecificEffectManagers;
using YouBoss.Common.Tools.Easings;
using Terraria.Audio;
using YouBoss.Assets;

namespace YouBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// Whether the terra blade is waiting for the current state to end so that it can move onto its death animation or not.
        /// </summary>
        public bool WaitingForDeathAnimation
        {
            get;
            set;
        }

        /// <summary>
        /// The amount of rings created by the death animation so far.
        /// </summary>
        public ref float DeathAnimation_RingCreationCounter => ref NPC.ai[0];

        /// <summary>
        /// The zoom value for the death animation.
        /// </summary>
        public ref float DeathAnimation_Zoom => ref NPC.ai[1];

        /// <summary>
        /// How long the player waits before creating glow rings during the terra blade's death animation.
        /// </summary>
        public static int DeathAnimation_GlowRingSpawnDelay => SecondsToFrames(0.5f);

        /// <summary>
        /// How long the player spends creating glow rings during the terra blade's death animation.
        /// </summary>
        public static int DeathAnimation_GlowRingSpawnTime => SecondsToFrames(1f);

        /// <summary>
        /// How rate at which glow rings are created during the terra blade's death animation.
        /// </summary>
        public static int DeathAnimation_GlowRingSpawnRate => SecondsToFrames(0.2667f);

        /// <summary>
        /// How long the terra blade waits before creating twinkles after the twinkle glow rings during the terra blade's death animation..
        /// </summary>
        public static int DeathAnimation_TwinkleDelay => SecondsToFrames(0.5f);

        /// <summary>
        /// How long the terra blade waits before exploding after the twinkle during the terra blade's death animation..
        /// </summary>
        public static int DeathAnimation_BurstDelay => SecondsToFrames(0.94f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_DeathAnimation()
        {
            StateMachine.AddTransitionStateHijack(originalState =>
            {
                if (WaitingForDeathAnimation)
                    return TerraBladeAIType.DeathAnimation;

                return originalState;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.DeathAnimation, DoBehavior_DeathAnimation);
        }

        public void DoBehavior_DeathAnimation()
        {
            // Disable damage.
            NPC.dontTakeDamage = true;

            // Slow down.
            NPC.velocity *= 0.84f;

            // Raise the sword upward.
            NPC.spriteDirection = 1;
            NPC.rotation = NPC.rotation.AngleLerp(-PiOver2, 0.08f);

            // Make afterimages and the like disappear.
            ShineInterpolant *= 0.7f;
            AfterimageOpacity *= 0.7f;

            // Get out of blocks.
            if (Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height + 32))
                NPC.position.Y -= 4f;

            // Zoom in on the blade.
            float idealZoom = DeathAnimation_RingCreationCounter * 0.07f;
            Vector2 playerOffset = (NPC.rotation + (NPC.spriteDirection == -1 ? -PiOver2 : 0f)).ToRotationVector2() * PlayerDrawOffsetFactor * -26f;
            CameraPanSystem.CameraFocusPoint = NPC.Center + playerOffset;
            CameraPanSystem.CameraPanInterpolant = SineEasing.Default.Evaluate(EasingType.Out, InverseLerp(0f, 24f, AITimer).Cubed()) * InverseLerp(143f, 140f, AITimer);
            CameraPanSystem.Zoom = CameraPanSystem.CameraPanInterpolant * DeathAnimation_Zoom;
            DeathAnimation_Zoom = Lerp(DeathAnimation_Zoom, idealZoom, 0.3f);

            // Create glow ring particles.
            bool canCreateGlowRings = AITimer >= DeathAnimation_GlowRingSpawnDelay && AITimer <= DeathAnimation_GlowRingSpawnDelay + DeathAnimation_GlowRingSpawnTime;
            if (canCreateGlowRings && AITimer % DeathAnimation_GlowRingSpawnRate == 0)
            {
                PerformVFXForMultiplayer(() =>
                {
                    Color ringColor = Color.Lerp(Color.Teal, Color.Yellow, Main.rand.NextFloat(0.75f));
                    PulseRingParticle ring = new(NPC.Center + playerOffset, ringColor * 0.4f, 2f, 0f, 24);
                    ring.Spawn();

                    BloomParticle bloom = new(NPC.Center + playerOffset, ringColor.HueShift(0.09f), 0.6f, 40);
                    bloom.Spawn();

                    StartShake(2.5f);
                });

                DeathAnimation_RingCreationCounter++;
                NPC.netUpdate = true;
            }

            // Create a twinkle on the player's position.
            if (AITimer == DeathAnimation_GlowRingSpawnDelay + DeathAnimation_GlowRingSpawnTime + DeathAnimation_TwinkleDelay)
            {
                // Shake the screen a bit.
                StartShake(4f);

                // Play a twinkle sound.
                SoundEngine.PlaySound(TwinkleSound);

                // Create twinkle particles.
                PerformVFXForMultiplayer(() =>
                {
                    LensFlareParticle twinkle = new(NPC.Center + playerOffset, Color.YellowGreen, 20, 0.01f)
                    {
                        ScaleExpandRate = 0.011f
                    };
                    twinkle.Spawn();

                    twinkle = new(NPC.Center + playerOffset, Color.Wheat, 20, 0f)
                    {
                        ScaleExpandRate = 0.01f
                    };
                    twinkle.Spawn();
                });

                // Make the player fade out a bit.
                PlayerOpacityFactor = 0.67f;
            }

            // Create pre-burst visuals.
            if (AITimer == DeathAnimation_GlowRingSpawnDelay + DeathAnimation_GlowRingSpawnTime + DeathAnimation_TwinkleDelay + DeathAnimation_BurstDelay - 2)
            {
                PerformVFXForMultiplayer(() =>
                {
                    MagicBurstParticle burst = new(NPC.Center + playerOffset, Vector2.Zero, new(225, 255, 242), 15, 1.3f);
                    burst.Spawn();

                    LensFlareParticle twinkle = new(NPC.Center + playerOffset, Color.White, 12, 0.1f, PiOver2)
                    {
                        ScaleExpandRate = 0.15f
                    };
                    twinkle.Spawn();
                });

                // Make the player completely disappear.
                PlayerOpacityFactor = 0f;
            }

            // Create burst visuals.
            if (AITimer == DeathAnimation_GlowRingSpawnDelay + DeathAnimation_GlowRingSpawnTime + DeathAnimation_TwinkleDelay + DeathAnimation_BurstDelay)
            {
                PerformVFXForMultiplayer(() =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ExpandingChromaticBurstParticle burst = new(NPC.Center + playerOffset, Vector2.Zero, Color.White, 16, i * 0.04f + 0.04f);
                        burst.Spawn();

                        BloomParticle bloom = new(NPC.Center + playerOffset, Color.Wheat * 0.71f, i * 1.35f + 4f, 25);
                        bloom.Spawn();
                    }

                    for (int i = 0; i < 90; i++)
                    {
                        int bloomLifetime = Main.rand.Next(240, 480);
                        float bloomScale = Main.rand.NextFloat(0.06f, 0.22f);
                        Vector2 bloomVelocity = new(Main.rand.NextFloat(4f, 160f) * Main.rand.NextFromList(-1f, 1f), -Lerp(1f, 90f, Main.rand.NextFloat().Cubed()));
                        Color bloomColor = Color.Lerp(Color.Lime, Color.Yellow, Main.rand.NextFloat());
                        bloomColor = Color.Lerp(bloomColor, Color.Wheat, 0.64f);

                        FallingBloomParticle bloom = new(NPC.Center + playerOffset, bloomVelocity, bloomColor, bloomScale, bloomLifetime);
                        bloom.Spawn();
                    }

                    // Shake the screen heavily.
                    StartShake(13.5f);
                    RadialScreenShoveSystem.Start(NPC.Center + playerOffset, 120);
                });

                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.PlayerDisappearSound, NPC.Center);
            }

            // Disappear.
            NPC.Opacity = Pow(InverseLerp(150f, 0f, AITimer - DeathAnimation_GlowRingSpawnDelay - DeathAnimation_GlowRingSpawnTime - DeathAnimation_TwinkleDelay - DeathAnimation_BurstDelay), 0.76f);
            if (NPC.Opacity < 1f)
            {
                // Return the night sky to its original state.
                PerformingStartAnimation = true;

                float magicSpawnChance = InverseLerpBump(0.25f, 0.6f, 0.9f, 1f, NPC.Opacity) * 0.7f;

                if (Main.rand.NextFloat() < magicSpawnChance)
                {
                    Dust magic = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(14f, 30f), 261);
                    magic.color = MulticolorLerp(Main.rand.NextFloat(0.9f), Color.Yellow, Color.Turquoise, Color.SkyBlue);
                    magic.fadeIn = 0.6f;
                    magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(1.5f);
                    magic.scale = 0.5f;
                    magic.noGravity = true;
                }
            }

            // Die once completely invisible.
            if (NPC.Opacity <= 0f)
            {
                NPC.Center = Target.Center - Vector2.UnitY * 600f;
                NPC.boss = false;
                NPC.life = 0;
                NPC.HitEffect();
                NPC.checkDead();
            }

            if (Main.mouseRightRelease && Main.mouseRight)
            {
                AITimer = 0;
                DeathAnimation_RingCreationCounter = 0f;
                PlayerOpacityFactor = 1f;
            }
        }

        public override bool CheckDead()
        {
            // Disallow natural death. The time check here is as a way of catching cases where multiple hits happen on the same frame and trigger a death.
            // If it just checked the attack state, then hit one would trigger the state change, set the HP to one, and the second hit would then deplete the
            // single HP and prematurely kill the boss.
            if (CurrentState == TerraBladeAIType.DeathAnimation && AITimer >= 10)
                return true;

            // Keep the terra blade's HP at its minimum.
            NPC.life = 1;

            if (!WaitingForDeathAnimation)
            {
                WaitingForDeathAnimation = true;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
            }
            return false;
        }
    }
}
