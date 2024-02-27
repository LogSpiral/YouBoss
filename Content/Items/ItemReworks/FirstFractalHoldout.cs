using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Core.Graphics.Shaders.Screen;
using static Humanizer.In;

namespace YouBoss.Content.Items.SummonItems
{
    public class FirstFractalHoldout : ModProjectile
    {
        public static int UseTime => SecondsToFrames(0.44f);

        public static int BaseDamage => 256;

        public float AnimationCompletion => Saturate(Time / UseTime);

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public ref float ForwardDirection => ref Projectile.ai[1];

        public ref float StartingRotation => ref Projectile.ai[2];

        public override string Texture => $"Terraria/Images/Item_{ItemID.FirstFractal}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 120;
            GlobalItemManager.SetDefaultsEvent += ChangeFirstFractalInitialization;
            GlobalItemManager.CanUseItemEvent += ChangeFirstFractalUseCondition;
        }

        private void ChangeFirstFractalInitialization(Item item)
        {
            if (item.type != ItemID.FirstFractal)
                return;

            item.width = 60;
            item.height = 60;
            item.damage = BaseDamage;
            item.useStyle = ItemUseStyleID.Swing;
            item.useTime = UseTime;
            item.useAnimation = UseTime;
            item.useTurn = true;
            item.DamageType = DamageClass.MeleeNoSpeed;
            item.knockBack = 8f;
            item.autoReuse = true;
            item.noUseGraphic = true;
            item.channel = true;
            item.shoot = Type;
            item.shootSpeed = 9f;
            item.rare = ItemRarityID.Purple;
        }

        private bool ChangeFirstFractalUseCondition(Item item, Player player)
        {
            if (item.type != ItemID.FirstFractal)
                return true;

            return player.ownedProjectileCounts[Type] <= 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.MaxUpdates = 1;
            Projectile.localNPCHitCooldown = UseTime;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            // Initialize directions.
            if (ForwardDirection == 0f)
            {
                StartingRotation = Projectile.velocity.ToRotation();
                ForwardDirection = Projectile.velocity.X.NonZeroSign();
            }

            DoBehavior_SwingForward();

            // Decide the arm rotation for the owner.
            float armRotation = Projectile.rotation - (ForwardDirection == 1f ? PiOver2 : Pi);
            Owner.SetCompositeArmFront(Math.Abs(armRotation) > 0.01f, Player.CompositeArmStretchAmount.Full, armRotation);

            // Glue the sword to its owner.
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir((int)ForwardDirection);

            // Increment time and disappear once the AI timer has reached its maximum.
            Time++;
            if (AnimationCompletion >= 1f)
                Projectile.Kill();
        }

        public void DoBehavior_SwingForward()
        {
            float swingRotationOffset = Lerp(-3.2f, 1.6f, AnimationCompletion.Cubed());
            Projectile.rotation = StartingRotation + swingRotationOffset * ForwardDirection;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the first fractal.
            float rotation = Projectile.rotation + PiOver4;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = Vector2.UnitY * texture.Size();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects direction = SpriteEffects.None;

            if (ForwardDirection == -1f)
            {
                origin.X = texture.Width - origin.X;
                direction = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), rotation, origin, Projectile.scale, direction);

            return false;
        }
    }
}
