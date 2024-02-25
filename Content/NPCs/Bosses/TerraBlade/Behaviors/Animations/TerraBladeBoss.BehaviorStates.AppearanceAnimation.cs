using Microsoft.Xna.Framework;
using YouBoss.Common.Tools.Reflection;
using YouBoss.Content.NPCs.Bosses.TerraBlade.Projectiles;
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
        /// Whether the terra blade has hit ground during its appearance animation.
        /// </summary>
        public bool AppearanceAnimation_HasHitGround
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        /// <summary>
        /// How long it's been since the terra blade hit grounding during its appearance animation.
        /// </summary>
        public ref float AppearanceAnimation_TimeSinceGroundHit => ref NPC.ai[1];

        /// <summary>
        /// How long the terra blade waits above the player before slamming downward.
        /// </summary>
        public static int AppearanceAnimation_SlamDownDelay => SecondsToFrames(0.5f);

        /// <summary>
        /// How long the terra blade waits in the ground the player before slamming transition to the next AI state during its appearance animation.
        /// </summary>
        public static int AppearanceAnimation_AttackTransitionDelay => SecondsToFrames(0.05f);

        /// <summary>
        /// The maximum speed at which the terra blade falls down during its appearance animation.
        /// </summary>
        public static float AppearanceAnimation_MaxFallSpeed => 96f;

        [AutomatedMethodInvoke]
        public void LoadStateTransitions_AppearanceAnimation()
        {
            StateMachine.RegisterTransition(TerraBladeAIType.AppearanceAnimation, TerraBladeAIType.StruggleOutOfBlocks, false, () =>
            {
                return AppearanceAnimation_HasHitGround && AppearanceAnimation_TimeSinceGroundHit >= AppearanceAnimation_AttackTransitionDelay;
            });

            // Load the AI state behavior.
            StateMachine.RegisterStateBehavior(TerraBladeAIType.AppearanceAnimation, DoBehavior_AppearanceAnimation);
        }

        public void DoBehavior_AppearanceAnimation()
        {
            PerformingStartAnimation = true;

            // Disable damage.
            NPC.dontTakeDamage = true;

            // Stay above and behind the target at first.
            if (AITimer <= AppearanceAnimation_SlamDownDelay)
            {
                float targetDirection = Target.Velocity.X.NonZeroSign();
                Vector2 hoverDestination = Target.Center + new Vector2(targetDirection * -270f, -1500f);
                NPC.Center = hoverDestination;

                // Point downward.
                NPC.rotation = PiOver2;
                NPC.spriteDirection = 1;
                return;
            }

            // Slow downward after the wait has concluded.
            if (!AppearanceAnimation_HasHitGround)
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitY * AppearanceAnimation_MaxFallSpeed, 0.095f);
            else
                AppearanceAnimation_TimeSinceGroundHit++;

            // Check for tile collisions is close to or below the target.
            bool registerTileCollisions = NPC.Center.Y >= Target.Center.Y - 300f;
            if (registerTileCollisions)
            {
                // Apply ground hit effects when ready.
                if (!AppearanceAnimation_HasHitGround && Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height, true))
                {
                    NPC.velocity.Y = 0f;
                    while (Collision.SolidCollision(NPC.TopLeft, NPC.width, NPC.height - 6, true))
                        NPC.position.Y -= 2f;
                    NPC.position.Y += 12f;

                    // Create impact effects.
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, NPC.Center);
                    StartShake(19f);
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 18);

                    // Shock the ground.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(BladeTip, Vector2.Zero, ModContent.ProjectileType<TerraGroundShock>(), 0, 0f);

                    AppearanceAnimation_HasHitGround = true;
                    NPC.netUpdate = true;
                }
            }
        }
    }
}
