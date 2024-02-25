using Microsoft.Xna.Framework;
using NoxusBoss.Assets;
using NoxusBoss.Common.Tools.Reflection;
using NoxusBoss.Content.NPCs.Bosses.TerraBlade.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        /// <summary>
        /// The current hover offset angle of the diamond sweep attack.
        /// </summary>
        public ref float DiamondSweeps_HoverOffsetAngle => ref NPC.ai[0];

        /// <summary>
        /// How long the diamond sweep attack should go on for.
        /// </summary>
        public int DiamondSweeps_AttackDuration => SecondsToFrames(ByPhase(1.2f, 1.25f, 1.4f));

        /// <summary>
        /// How long the diamond sweep attack should wait after concluding before transitioning to the next attack.
        /// </summary>
        public static int DiamondSweeps_AttackTransitionDelay => SecondsToFrames(1.1f);

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_DiamondSweeps()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.DiamondSweeps, null, false, () =>
            {
                return AITimer >= DiamondSweeps_AttackDuration + DiamondSweeps_AttackTransitionDelay;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.DiamondSweeps, DoBehavior_DiamondSweeps);
        }

        public float CalculateDiamondSweepsAttackWeight()
        {
            // Make this attack not happen if the previous attack was the beam spin, to allow melee-specific attacks to shine after that instead.
            if (PreviousState == TerraBladeAIType.EnergyBeamSpin)
                return 0f;

            return 1f;
        }

        public void DoBehavior_DiamondSweeps()
        {
            float flySpeedInterpolant = InverseLerp(0f, 24f, AITimer);
            Vector2 hoverOffsetDirection = DiamondSweeps_HoverOffsetAngle.ToRotationVector2();
            hoverOffsetDirection.X = Asin(hoverOffsetDirection.X) * 2f / Pi;
            hoverOffsetDirection.Y = Asin(hoverOffsetDirection.Y) * 2f / Pi;

            Vector2 hoverDestination = Target.Center - hoverOffsetDirection * new Vector2(640f, 560f);
            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.015f);
            NPC.SmoothFlyNear(hoverDestination, flySpeedInterpolant * 0.5f, 0.5f);

            if (AITimer == 1)
            {
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
                DiamondSweeps_HoverOffsetAngle = NPC.AngleFrom(Target.Center) + Pi;
                NPC.netUpdate = true;
            }

            // Prepare for the swipe.
            float sweepSpeedWindup = InverseLerpBump(0f, 30f, DiamondSweeps_AttackDuration, DiamondSweeps_AttackDuration + 20f, AITimer) * RemapPow(DiamondSweeps_HoverOffsetAngle % PiOver2, 0.3f, PiOver2 - 0.4f, PiOver2, 1f, 0.05f);
            bool canReleaseBeams = AITimer < DiamondSweeps_AttackDuration && !NPC.WithinRange(Target.Center, 350f) && sweepSpeedWindup >= 0.05f;
            if (canReleaseBeams && NPC.soundDelay <= 0 && DiamondSweeps_HoverOffsetAngle % PiOver2 < 0.05f)
            {
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound, NPC.Center);
                NPC.oldPos = new Vector2[NPC.oldPos.Length];
                NPC.soundDelay = 8;
            }

            // Release beams.
            if (Main.netMode != NetmodeID.MultiplayerClient && canReleaseBeams && AITimer % 3 == 0)
                NewProjectileBetter(NPC.Center, NPC.DirectionToSafe(Target.Center).RotatedByRandom(0.13f) * 0.5f, ModContent.ProjectileType<AcceleratingTerraBeam>(), TerraBeamDamage, 0f);

            // Release a bunch of sparkle particles.
            if (canReleaseBeams)
            {
                ParticleOrchestraSettings particleSettings = new()
                {
                    PositionInWorld = NPC.Center + NPC.rotation.ToRotationVector2() * 104f,
                    MovementVector = (NPC.rotation + PiOver2).ToRotationVector2() * 7.5f + Main.rand.NextVector2Circular(6f, 6f)
                };
                ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.TerraBlade, particleSettings);
            }

            // Spin around the target.
            float defaultSweepArc = TwoPi / DiamondSweeps_AttackDuration * 3f;
            DiamondSweeps_HoverOffsetAngle += defaultSweepArc * sweepSpeedWindup;

            // Use afterimages and gleam effects when beams are being fired.
            ShineInterpolant = InverseLerp(0f, 10f, NPC.velocity.Length());
            AfterimageOpacity = ShineInterpolant;
            AfterimageClumpInterpolant = 0.2f;

            // Look at the target.
            float idealRotation = NPC.AngleTo(Target.Center);
            NPC.spriteDirection = (Target.Center.X - NPC.Center.X).NonZeroSign();
            if (NPC.spriteDirection == -1)
                idealRotation += PiOver2;
            NPC.rotation = NPC.rotation.AngleLerp(idealRotation, 0.3f);
        }
    }
}
