using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.TerraBlade.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.TerraBlade
{
    public partial class TerraBladeBoss : ModNPC
    {
        #region Custom Types and Enumerations
        public enum TerraBladeAIType
        {
            // Animation states.
            AppearanceAnimation,
            StruggleOutOfBlocks,
            ThreateninglyAimAtTarget,
            EnterPhase2,
            EnterPhase3,

            // Phase 1 Attacks.
            SingleSwipe,
            RapidDashes,
            MorphoKnightLungeSweeps,
            EnergyBeamSpin,
            DashSpin,

            // Phase 2 attacks.
            TelegraphedBeamDashes,
            DiamondSweeps,

            // Phase 3 Attacks.
            AerialSwoopDashes,
            BreakIntoTrueBlades,

            // Technical behaviors.
            ResetCycle,

            // Used solely for iteration.
            Count
        }

        #endregion Custom Types and Enumerations

        #region Fields and Properties
        private static NPC myself;

        /// <summary>
        /// The amount of extra stars that should exist in the sky. The fractional part of this value corresponds to the visibility of the last star.
        /// </summary>
        public float ExtraStarsInSkyCount
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the terra blade should be updated via its velocity.
        /// </summary>
        public bool CanMove
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the terra blade is currently performing its starting animation.
        /// </summary>
        public bool PerformingStartAnimation
        {
            get;
            set;
        }

        /// <summary>
        /// How many frames it's been since Noxus' fight began.
        /// </summary>
        public int FightDuration
        {
            get;
            set;
        }

        /// <summary>
        /// Noxus' life to max life ratio.
        /// </summary>
        public float LifeRatio => NPC.life / (float)NPC.lifeMax;

        /// <summary>
        /// Noxus' target.
        /// </summary>
        public NPCAimedTarget Target => NPC.GetTargetData();

        public Vector2 BladeTip => NPC.Center + NPC.rotation.ToRotationVector2() * NPC.scale * 42f;

        /// <summary>
        /// The AI timer for Noxus' current state.
        /// </summary>
        /// <remarks>
        /// Notably, <i>AI timers are local to a given state</i>.
        /// </remarks>
        public ref int AITimer => ref StateMachine.CurrentState.Time;

        /// <summary>
        /// The current AI state Noxus is using. This uses the <see cref="StateMachine"/> under the hood.
        /// </summary>
        public TerraBladeAIType CurrentState
        {
            get
            {
                PerformStateSafetyCheck();
                return StateMachine?.CurrentState?.Identifier ?? TerraBladeAIType.AppearanceAnimation;
            }
        }

        /// <summary>
        /// Noxus' <see cref="NPC"/> instance. Returns <see langword="null"/> if Noxus is not present.
        /// </summary>
        public static NPC Myself
        {
            get
            {
                if (myself is not null && !myself.active)
                    return null;

                return myself;
            }
            private set => myself = value;
        }

        #endregion Fields and Properties

        #region Initialization

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 90;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Scale = 0.7f,
                PortraitScale = 0.7f
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.UsesNewTargetting[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            // Apply miracleblight immunities.
            NPC.MakeImmuneToMiracleblight();

            if (Main.netMode != NetmodeID.Server)
            {
                // Initialize the sky.
                SkyManager.Instance[TerraBladeSky.SkyKey] = new TerraBladeSky();

                // Prepare render targets.
                PlayerDrawContents = new();
                Main.ContentThatNeedsRenderTargets.Add(PlayerDrawContents);
            }

            // Load textures.
            LoadTextures();

            On_Main.DrawNPCHeadBoss += DrawPlayerHeadOnMap;
        }

        private void DrawPlayerHeadOnMap(On_Main.orig_DrawNPCHeadBoss orig, Entity npc, byte alpha, float headScale, float rotation, SpriteEffects direction, int bossHeadId, float x, float y)
        {
            if (npc is NPC n && n.type == Type)
            {
                float opacity = alpha / 255f * n.Opacity * n.As<TerraBladeBoss>().PlayerAppearanceInterpolant;
                if (opacity <= 0f)
                    return;

                Color borderColor = Main.GetPlayerHeadBordersColor(Main.LocalPlayer) * opacity;
                Vector2 headDrawPosition = new(x, y);

                int oldDirection = Main.LocalPlayer.direction;
                Main.LocalPlayer.direction = AngleToXDirection(n.rotation);
                Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, Main.LocalPlayer, headDrawPosition.Floor(), opacity, headScale, borderColor);
                Main.LocalPlayer.direction = oldDirection;

                return;
            }

            orig(npc, alpha, headScale, rotation, direction, bossHeadId, x, y);
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 50f;
            NPC.damage = 230;
            NPC.width = 46;
            NPC.height = 46;
            NPC.defense = 50;
            NPC.SetLifeMaxByMode(900000, 1000000, 1123200);

            if (Main.expertMode)
            {
                NPC.damage = 275;

                // Undo vanilla's automatic Expert boosts.
                NPC.lifeMax /= 2;
                NPC.damage /= 2;
            }

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.boss = true;
            NPC.hide = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.value = Item.buyPrice(2, 0, 0, 0) / 5;
            NPC.netAlways = true;
            NPC.MakeCalamityBossBarClose();
            Music = MusicID.Boss4;
        }

        #endregion Initialization

        #region Networking

        public override void SendExtraAI(BinaryWriter writer)
        {
            // Write boolean data.
            BitsByte b1 = new()
            {
                [0] = CanMove,
                [1] = PerformingStartAnimation,
                [2] = InPhase2,
            };
            writer.Write(b1);

            // Write float data.
            writer.Write(NPC.Opacity);
            writer.Write(NPC.rotation);
            writer.Write(ExtraStarsInSkyCount);

            // Send vector data.
            writer.WriteVector2(MorphoKnightLungeSweeps_SlashDirection);
            writer.WriteVector2(SingleSwipe_SwipeDestination);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            // Read boolean data.
            BitsByte b1 = reader.ReadByte();
            CanMove = b1[0];
            PerformingStartAnimation = b1[1];
            InPhase2 = b1[2];

            // Read float data.
            NPC.Opacity = reader.ReadSingle();
            NPC.rotation = reader.ReadSingle();
            ExtraStarsInSkyCount = reader.ReadSingle();

            // Read vector data.
            MorphoKnightLungeSweeps_SlashDirection = reader.ReadVector2();
            SingleSwipe_SwipeDestination = reader.ReadVector2();
        }

        #endregion Networking

        #region AI
        public override void AI()
        {
            // Pick a target if the current one is invalid.
            bool invalidTargetIndex = Target.Invalid;
            if (invalidTargetIndex)
                NPC.TargetClosestUpgraded();

            if (!NPC.WithinRange(Target.Center, 4972f))
                NPC.TargetClosestUpgraded();

            // Hey bozo the player's gone. Leave.
            if (Target.Invalid)
                NPC.active = false;

            // Perform a state safety check before anything else.
            PerformStateSafetyCheck();

            // Grant the target infinite flight and ensure that they receive the boss effects buff.
            if (NPC.HasPlayerTarget)
            {
                Player playerTarget = Main.player[NPC.target];
                playerTarget.wingTime = playerTarget.wingTimeMax;
                playerTarget.GrantInfiniteFlight();
                playerTarget.GrantBossEffectsBuff();
            }

            // Disable rain and sandstorms.
            Main.StopRain();
            if (Main.netMode != NetmodeID.MultiplayerClient && Sandstorm.Happening)
                Sandstorm.StopSandstorm();

            // Set the global NPC instance.
            Myself = NPC;

            // Reset things every frame.
            PlayerDrawOffsetFactor = Saturate(PlayerDrawOffsetFactor + 0.06f);
            OriginOffset *= 0.95f;
            CanMove = true;
            PerformingStartAnimation = false;
            AfterimageTrailCompletion = Saturate(AfterimageTrailCompletion - 0.1f);
            NPC.damage = 0;
            NPC.defense = NPC.defDefense;
            NPC.immortal = false;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;
            NPC.noTileCollide = true;

            // Make a ton of stars appear in the sky in the third phase.
            if (InPhase3)
                ExtraStarsInSkyCount = Clamp(ExtraStarsInSkyCount + 0.09f, 0f, 360f); ;

            // Handle AI behaviors.
            StateMachine.PerformBehaviors();

            // Use the target's name if not in the starting animation.
            if (!PerformingStartAnimation)
                NPC.GivenName = Main.LocalPlayer.name;

            // Update the state machine.
            StateMachine.PerformStateTransitionCheck();

            // Do not despawn.
            NPC.timeLeft = 7200;

            // Increment timers.
            PerformStateSafetyCheck();
            AITimer++;
            FightDuration++;

            // Lock the time.
            if (Main.dayTime)
            {
                Main.dayTime = false;
                Main.time = 0D;
            }
            Main.time = Lerp((float)Main.time, 16200f, 0.15f);

            // Get rid of all falling stars. Their noises completely ruin the ambience.
            // active = false must be used over Kill because the Kill method causes them to drop their fallen star items.
            var fallingStars = AllProjectilesByID(ProjectileID.FallingStar);
            foreach (Projectile star in fallingStars)
                star.active = false;

            // Use a full moon.
            Main.moonPhase = 0;

            // Apparently there's no ShouldUpdatePosition hook for NPCs?
            if (!CanMove)
                NPC.Center -= NPC.velocity;
        }

        /// <summary>
        /// Selects a floating point value based on the terra blade's current phase.
        /// </summary>
        /// <param name="phase1Value">The value to select in phase 1.</param>
        /// <param name="phase2Value">The value to select in phase 2.</param>
        /// <param name="phase3Value">The value to select in phase 3.</param>
        public float ByPhase(float phase1Value, float phase2Value, float phase3Value)
        {
            if (InPhase3)
                return phase3Value;
            if (InPhase2)
                return phase2Value;

            return phase1Value;
        }

        #endregion AI
    }
}
