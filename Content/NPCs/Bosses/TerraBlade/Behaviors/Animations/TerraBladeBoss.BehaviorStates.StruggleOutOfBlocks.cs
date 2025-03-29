﻿using Microsoft.Xna.Framework;
using YouBoss.Common.Tools.Easings;
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
        /// The horizontal direction in which the terra blade is struggling.
        /// </summary>
        public ref float StruggleOutOfBlocks_StruggleDirection => ref NPC.ai[0];

        /// <summary>
        /// The totalTime used to StruggleOutOfBlocks
        /// </summary>
        public ref float StruggleOutOfBlocks_TotalUsedTime => ref NPC.ai[1];

        /// <summary>
        /// Max struggle time so that It won't struggle under plenty of tiles
        /// </summary>
        public const float MAXSTRUGGLETIME = 180;

        /// <summary>
        /// How long the terra blade should be free to float for after struggling outside of blocks.
        /// </summary>
        public static int StruggleOutOfBlocks_FloatTime => SecondsToFrames(0.45f);

        /// <summary>
        /// The acceleration at which the terra blade floats upward after struggling outside of blocks.
        /// </summary>
        public static float StruggleOutOfBlocks_FloatAcceleration => 0.32f;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_StruggleOutOfBlocks()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.StruggleOutOfBlocks, TerraBladeAIType.ThreateninglyAimAtTarget, false, () =>
            {
                return Abs(NPC.velocity.Y) >= StruggleOutOfBlocks_FloatTime * StruggleOutOfBlocks_FloatAcceleration;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.StruggleOutOfBlocks, DoBehavior_StruggleOutOfBlocks);
        }

        public void DoBehavior_StruggleOutOfBlocks()
        {
            PerformingStartAnimation = true;

            // Disable damage.
            NPC.dontTakeDamage = true;
            StruggleOutOfBlocks_TotalUsedTime++;
            // Prevent natural movement in the tiles while stuck.
            // Add a timer so that it won't stuck in plenty of tiles for a VERY long time
            bool stuck = Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height - 4, true);
            if (stuck && StruggleOutOfBlocks_TotalUsedTime < MAXSTRUGGLETIME)
            {
                CanMove = false;
                OriginOffset = new Vector2(0.5f, -0.5f);

                // Define rotation.
                NPC.velocity.Y = 1.42f;
                NPC.velocity = (NPC.velocity * new Vector2(0.95f, 1f)).SafeNormalize(Vector2.UnitY);
            }
            else
            {
                NPC.velocity.X *= 0.8f;
                NPC.velocity.Y -= StruggleOutOfBlocks_FloatAcceleration;
                OriginOffset = Vector2.Zero;
            }

            // Keep the camera on the blade.
            CameraPanSystem.CameraPanInterpolant = PolynomialEasing.Cubic.Evaluate(EasingType.InOut, InverseLerp(0f, 12f, AITimer));
            CameraPanSystem.CameraFocusPoint = NPC.Center;

            // Shake in place.
            StruggleOutOfBlocks_StruggleDirection *= 0.8756f;
            NPC.velocity.X += Main.rand.NextFloat(0.06f, 0.1f) * StruggleOutOfBlocks_StruggleDirection;

            // Create dust in the blocks as an indication of the struggle.
            if (Abs(StruggleOutOfBlocks_StruggleDirection) >= 0.25f)
            {
                PerformVFXForMultiplayer(() =>
                {
                    Collision.HitTiles(NPC.Bottom, -Vector2.UnitY.RotatedByRandom(0.6f) * 15f, 16, 16);
                });
            }

            // Randomly struggle in some direction to escape the blocks.
            if (AITimer % 38 == 0 && stuck)
            {
                SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact, NPC.Center);

                StruggleOutOfBlocks_StruggleDirection = Main.rand.NextFloat(0.8f, 1.6f) * Main.rand.NextFromList(-1f, 1f);
                NPC.position.Y -= Main.rand.NextFloat(3f, 4f);
                NPC.netUpdate = true;
            }

            // Define rotation.
            if (stuck)
                NPC.rotation = NPC.velocity.ToRotation();
            else
                NPC.rotation = NPC.rotation.AngleTowards(PiOver2, 0.4f);
        }
    }
}
