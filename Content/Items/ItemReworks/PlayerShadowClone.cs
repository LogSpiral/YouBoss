using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace YouBoss.Content.Items.ItemReworks
{
    public class PlayerShadowClone : ModProjectile
    {
        /// <summary>
        /// Whether the clone has played its summoning sound yet or not.
        /// </summary>
        public bool HasPlayedSummonSound
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }

        /// <summary>
        /// How long the clone has existed, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// How long the clone lingers for.
        /// </summary>
        public static int Lifetime => TerraSlash.Lifetime;

        public override string Texture => "YouBoss/Content/Items/ItemReworks/FirstFractal";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 15;
            Projectile.noEnchantmentVisuals = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
            float lifetimeRatio = Time / Lifetime;
            Projectile.Opacity = InverseLerpBump(0f, 0.15f, 0.5f, 1f, lifetimeRatio).Squared();

            // Slow down and arc a bit as the animation goes on.
            if (lifetimeRatio >= 0.3f)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(Sin(Projectile.identity) * 0.021f);
                Projectile.velocity *= 0.85f;
            }

            // Rotate forward.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

            // Create a sound on the first frame.
            if (!HasPlayedSummonSound)
            {
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with { Volume = 3f, MaxInstances = 10 }, Projectile.Center);
                HasPlayedSummonSound = true;
            }

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public void DrawClone()
        {
            Main.spriteBatch.PrepareForShaders();

            int owner = Projectile.owner;
            Player other = Main.player[owner];
            Player player = Main.playerVisualClone[owner] ??= new();
            player.CopyVisuals(other);
            player.isFirstFractalAfterImage = true;
            player.firstFractalAfterImageOpacity = Projectile.Opacity * 1f;
            player.ResetEffects();
            player.ResetVisibleAccessories();
            player.UpdateDyes();
            player.DisplayDollUpdate();
            player.UpdateSocialShadow();
            player.itemAnimationMax = 60;
            player.itemAnimation = (int)Projectile.localAI[0];
            player.itemRotation = Projectile.velocity.ToRotation();
            player.Center = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 62f;
            player.direction = ((Projectile.velocity.X > 0f) ? 1 : -1);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - PiOver2);
            player.velocity.Y = 0.01f;
            player.wingFrame = 2;
            player.PlayerFrame();
            player.socialIgnoreLight = true;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, player, player.position, 0f, player.fullRotationOrigin, 0f, 1f);

            Main.spriteBatch.ResetToDefault();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawClone();
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White, positionClumpInterpolant: 0.56f);
            return false;
        }
    }
}
