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
        /// The current direction the terra blade is using during the morpho knight lunge sweeps.
        /// </summary>
        public Vector2 MorphoKnightLungeSweeps_SlashDirection
        {
            get;
            set;
        }

        /// <summary>
        /// How long the morpho knight lunge swipes should wait before occurring.
        /// </summary>
        public static int MorphoKnightLungeSweeps_SwipeHoverTime => SecondsToFrames(0.85f);

        /// <summary>
        /// How long the morpho knight lunge swipes should last.
        /// </summary>
        public static int MorphoKnightLungeSweeps_SwipeTime => SecondsToFrames(0.1833f);

        /// <summary>
        /// How long the morpho knight lunge swipes should wait after a swipe.
        /// </summary>
        public static int MorphoKnightLungeSweeps_WaitTime => SecondsToFrames(0.1667f);

        /// <summary>
        /// How long the terra blades after morpho knight lunge swipes attack concludes to begin the next attack.
        /// </summary>
        public static int MorphoKnightLungeSweeps_AttackTransitionTime => SecondsToFrames(0.85f);

        /// <summary>
        /// How many swipes to perform during the morpho knight lunge sweeps.
        /// </summary>
        public static int MorphoKnightLungeSweeps_TotalSwipes => 4;

        /// <summary>
        /// The amount of damage terra beam projectiles do.
        /// </summary>
        public static int TerraBeamDamage => Main.expertMode ? 250 : 210;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_MorphoKnightLungeSweeps()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.MorphoKnightLungeSweeps, null, false, () =>
            {
                return AITimer >= MorphoKnightLungeSweeps_SwipeHoverTime + (MorphoKnightLungeSweeps_SwipeTime + MorphoKnightLungeSweeps_WaitTime) * MorphoKnightLungeSweeps_TotalSwipes + MorphoKnightLungeSweeps_AttackTransitionTime;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.MorphoKnightLungeSweeps, DoBehavior_MorphoKnightLungeSweeps);
        }

        public float CalculateMorphoKnightLungeSweepsAttackWeight()
        {
            // Disallow this attack from happening if the player is quite far away, as that could lead to cheap hits due to how fast it'd need to move to catch up.
            if (!NPC.WithinRange(Target.Center, 750f))
                return 0f;

            // Disallow this attack from happening if the player is quite close away, as that could lead to cheap hits due to how little room there is to get out of the way.
            if (NPC.WithinRange(Target.Center, 240f))
                return 0f;

            // Disallow this attack from happening after a single swipe, since this attack has a similar premise in terms of movement.
            if (PreviousState == TerraBladeAIType.SingleSwipe)
                return 0f;

            return 1f;
        }

        public void DoBehavior_MorphoKnightLungeSweeps()
        {
            bool doneAttacking = AITimer >= MorphoKnightLungeSweeps_SwipeHoverTime + (MorphoKnightLungeSweeps_SwipeTime + MorphoKnightLungeSweeps_WaitTime) * MorphoKnightLungeSweeps_TotalSwipes;
            if (AITimer < MorphoKnightLungeSweeps_SwipeHoverTime || doneAttacking)
            {
                float idealRotation = NPC.AngleFrom(Target.Center);
                bool willAttackSoon = AITimer < MorphoKnightLungeSweeps_SwipeHoverTime;

                // Create a shine.
                if (willAttackSoon)
                    ShineInterpolant = InverseLerp(0f, 8f, AITimer);
                if (doneAttacking)
                    idealRotation += Pi;

                Vector2 hoverDestination = Target.Center + (NPC.Center - Target.Center).SafeNormalize(Vector2.UnitY) * 550f;
                NPC.SmoothFlyNear(hoverDestination, 0.18f, 0.85f);
                NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.15f);
                return;
            }

            // Enable contact damage.
            NPC.damage = NPC.defDamage;

            int swingTimer = (AITimer - MorphoKnightLungeSweeps_SwipeHoverTime) % (MorphoKnightLungeSweeps_SwipeTime + MorphoKnightLungeSweeps_WaitTime);
            if (swingTimer == 1)
            {
                // Play a swipe sound.
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.7f }, NPC.Center);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SlashSound, NPC.Center);
                SoundEngine.PlaySound(TwinkleSound with { Volume = 2f }, NPC.Center);

                // Choose the swing direction.
                if (AITimer <= MorphoKnightLungeSweeps_SwipeHoverTime + 5)
                {
                    NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();
                    MorphoKnightLungeSweeps_SlashDirection = NPC.DirectionToSafe(Target.Center + Target.Velocity * 14f);
                    NPC.rotation = MorphoKnightLungeSweeps_SlashDirection.ToRotation() + Pi;
                }
                else
                    NPC.direction *= -1;

                // Calculate the swipe destination.
                Vector2 startingDirection = -NPC.rotation.ToRotationVector2();
                SingleSwipe_SwipeDestination = NPC.Center + startingDirection * 240f;

                // Immediately reset all prior moment.
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;

                // Shake the screen a bit.
                StartShakeAtPoint(NPC.Center, 7f, shakeStrengthDissipationIncrement: 0.45f);

                // Reset the trail cache.
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
            }

            // Use the shine when going super fast.
            ShineInterpolant = InverseLerp(20f, 67f, NPC.velocity.Length());

            if (swingTimer >= 1 && swingTimer <= MorphoKnightLungeSweeps_SwipeTime + 1)
            {
                // Use afterimages.
                AfterimageOpacity = 1f;
                AfterimageClumpInterpolant = 0.15f;

                // Perform the swipe motion.
                float swipeArc = Pi / MorphoKnightLungeSweeps_SwipeTime * NPC.direction;
                Vector2 originalCenter = NPC.Center;
                Vector2 forwardForce = MorphoKnightLungeSweeps_SlashDirection * Remap(swingTimer, 0f, 4f, 160f, 84f);
                NPC.Center = SingleSwipe_SwipeDestination + (originalCenter - SingleSwipe_SwipeDestination).RotatedBy(swipeArc).SafeNormalize(Vector2.Zero) * 190f;
                NPC.velocity = forwardForce;

                // Release a bunch of sparkle particles.
                PerformVFXForMultiplayer(() =>
                {
                    ParticleOrchestraSettings particleSettings = new()
                    {
                        PositionInWorld = NPC.Center + (originalCenter - NPC.Center).SafeNormalize(Vector2.Zero) * 104f,
                        MovementVector = (NPC.Center - originalCenter) * 0.06f + Main.rand.NextVector2Circular(6f, 6f)
                    };
                    ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.TerraBlade, particleSettings);
                });

                // Release terra beams.
                if (Main.netMode != NetmodeID.MultiplayerClient && swingTimer >= 1)
                {
                    float terraBeamSpeed = 1.2f;
                    if (swingTimer % 2 == 1)
                        terraBeamSpeed *= 0.15f;
                    NewProjectileBetter(BladeTip, NPC.rotation.ToRotationVector2() * terraBeamSpeed, ModContent.ProjectileType<AcceleratingTerraBeam>(), TerraBeamDamage, 0f);
                }

                // Rotate forward.
                NPC.rotation = NPC.AngleFrom(SingleSwipe_SwipeDestination);
            }
            else
            {
                NPC.velocity *= 0.67f;
                AfterimageOpacity *= 0.7f;
            }
        }
    }
}
