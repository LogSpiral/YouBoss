using Microsoft.Xna.Framework;
using YouBoss.Assets;
using YouBoss.Common.Tools.Reflection;
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
        /// The arc angle of the single swipe for the terra blade.
        /// </summary>
        public ref float SingleSwipe_ArcAngle => ref NPC.ai[0];

        /// <summary>
        /// The destination of the swipe for the terra blade.
        /// </summary>
        public Vector2 SingleSwipe_SwipeDestination
        {
            get;
            set;
        }

        /// <summary>
        /// How long the single swipe should last.
        /// </summary>
        public static int SingleSwipe_SwipeTime => SecondsToFrames(0.1833f);

        /// <summary>
        /// How long the terra blade should wait after the single swipe.
        /// </summary>
        public static int SingleSwipe_AttackTransitionDelay => SecondsToFrames(0.64f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_SingleSwipe()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.SingleSwipe, null, false, () =>
            {
                return AITimer >= SingleSwipe_SwipeTime + SingleSwipe_AttackTransitionDelay;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.SingleSwipe, DoBehavior_SingleSwipe);
        }

        public float CalculateSingleSwipeAttackWeight()
        {
            // Disallow this attack from happening if the player is quite far away, as that could lead to cheap hits due to how fast it'd need to move to catch up.
            if (!NPC.WithinRange(Target.Center, 570f))
                return 0f;

            // Disallow this attack from happen after a chain sweeps, since this attack has a similar premise in terms of movement.
            if (PreviousState == TerraBladeAIType.MorphoKnightLungeSweeps)
                return 0f;

            // These attacks have problems with this one due to how the blade rotates after them.
            if (PreviousState == TerraBladeAIType.DashSpin)
                return 0f;

            return 1.3f;
        }

        public void DoBehavior_SingleSwipe()
        {
            // Enable contact damage.
            NPC.damage = NPC.defDamage;

            if (AITimer == 1)
            {
                // Play a swipe sound.
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.7f }, NPC.Center);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SlashSound, NPC.Center);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound, NPC.Center);

                // Choose the swing direction.
                NPC.direction = (Target.Center.X - NPC.Center.X).NonZeroSign();

                // Calculate the swipe destination.
                float swipeOffset = Clamp(NPC.Distance(Target.Center + Target.Velocity * 6f) * (0.47f + NPC.direction * 0.03f), 80f, 850f);
                if (NPC.direction == -1)
                    swipeOffset += 24f;

                Vector2 startingDirection = -NPC.rotation.ToRotationVector2();
                SingleSwipe_SwipeDestination = NPC.Center + startingDirection * swipeOffset;

                // Calculate the swipe angle.
                Vector2 desiredDirection = Target.Center.DirectionTo(SingleSwipe_SwipeDestination);
                SingleSwipe_ArcAngle = startingDirection.AngleBetween(desiredDirection);
                
                // Immediately reset all prior moment.
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;

                // Shake the screen a bit.
                StartShakeAtPoint(NPC.Center, 11f, shakeStrengthDissipationIncrement: 0.45f);

                // Reset the trail cache.
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
            }

            // Use the shine when going super fast.
            ShineInterpolant = InverseLerp(20f, 67f, NPC.velocity.Length());

            if (AITimer >= 1 && AITimer <= SingleSwipe_SwipeTime + 1)
            {
                // Use afterimages.
                AfterimageOpacity = 1f;
                AfterimageClumpInterpolant = 0.3f;

                // Perform the swipe motion.
                float swipeArc = SingleSwipe_ArcAngle / SingleSwipe_SwipeTime * NPC.direction;
                Vector2 originalCenter = NPC.Center;
                NPC.Center = SingleSwipe_SwipeDestination + (originalCenter - SingleSwipe_SwipeDestination).RotatedBy(swipeArc);
                NPC.velocity = NPC.Center - originalCenter;
                NPC.position -= NPC.velocity;

                // Release a bunch of sparkle particles.
                ParticleOrchestraSettings particleSettings = new()
                {
                    PositionInWorld = NPC.Center,
                    MovementVector = NPC.velocity * 0.06f + Main.rand.NextVector2Circular(5f, 5f)
                };
                ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.TerraBlade, particleSettings);

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
