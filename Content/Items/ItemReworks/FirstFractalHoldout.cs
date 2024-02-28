﻿using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Common.Tools.Easings;
using YouBoss.Core.Graphics.Shaders;
using static YouBoss.Content.Items.SummonItems.FirstFractal;

namespace YouBoss.Content.Items.SummonItems
{
    public class FirstFractalHoldout : ModProjectile
    {
        /// <summary>
        /// The rotation of the sword in 3D space.
        /// </summary>
        public Quaternion Rotation
        {
            get;
            set;
        }

        /// <summary>
        /// The vanish timer for this sword. As it increments, the sword decreases in size.
        /// </summary>
        public int VanishTimer
        {
            get;
            set;
        }

        /// <summary>
        /// How many swings have been performed thus far.
        /// </summary>
        public int SwingCounter
        {
            get;
            set;
        }

        /// <summary>
        /// The animation completion of the current sword swing.
        /// </summary>
        public float AnimationCompletion => Saturate(Time / UseTime);

        /// <summary>
        /// The owner of this sword.
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// The animation timer of this sword, in frame.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// The horizontal -1/1 direction that this sword is facing.
        /// </summary>
        public ref float HorizontalDirection => ref Projectile.ai[1];

        /// <summary>
        /// The starting Z rotation of this sword at the time of the animation starting.
        /// </summary>
        public ref float StartingRotation => ref Projectile.ai[2];

        /// <summary>
        /// The quad vertices responsible for drawing the sword.
        /// </summary>
        public static VertexPositionColorTexture[] SwordQuad
        {
            get;
            private set;
        }

        /// <summary>
        /// The indices associated with the drawn quads.
        /// </summary>
        public static readonly short[] QuadIndices = [0, 1, 2, 2, 3, 0];

        public override string Texture => $"Terraria/Images/Item_{ItemID.FirstFractal}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 120;
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
            Projectile.localNPCHitCooldown = UseTime;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(SwingCounter);
            writer.Write(Rotation.X);
            writer.Write(Rotation.Y);
            writer.Write(Rotation.Z);
            writer.Write(Rotation.W);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SwingCounter = reader.ReadInt32();

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();
            Rotation = new(x, y, z, w);
        }

        public override void AI()
        {
            // Initialize directions.
            if (HorizontalDirection == 0f)
            {
                StartingRotation = Projectile.velocity.ToRotation();
                HorizontalDirection = Projectile.velocity.X.NonZeroSign();
                Time = 0f;
                Projectile.netUpdate = true;
            }

            // Stick to the owner.
            StickToOwner();

            // Vanish if necessary.
            if (VanishTimer >= 1)
            {
                DoBehavior_Vanish();
                return;
            }

            // Handle slash behaviors.
            HandleSlashes();

            // Increment time and disappear once the AI timer has reached its maximum.
            Time++;
            if (Main.myPlayer == Projectile.owner && AnimationCompletion >= 1f)
            {
                // Check if the player is still using the sword. If they are, simply reset the AI timer.
                // Otherwise, die.
                if (Main.mouseLeft)
                {
                    Projectile.velocity = Projectile.DirectionToSafe(Main.MouseWorld);
                    SwingCounter++;

                    if (SwingCounter % 3 == 0)
                    {
                        StartingRotation = Projectile.velocity.ToRotation();
                        HorizontalDirection = Projectile.velocity.X.NonZeroSign();
                    }

                    Time = 0f;
                    Projectile.netUpdate = true;
                }
                else
                {
                    VanishTimer = 1;
                    Projectile.netUpdate = true;
                }
            }
        }

        public void HandleSlashes()
        {
            Quaternion forwardStart = EulerAnglesConversion(-0.06f, 0.12f);
            Quaternion forwardAnticipation = EulerAnglesConversion(-1.96f, -0.47f);
            Quaternion forwardSlash = EulerAnglesConversion(2.65f, -0.8f);
            Quaternion forwardEnd = EulerAnglesConversion(3.95f, 0.15f);
            Quaternion upwardSlash = EulerAnglesConversion(-0.06f, 0.45f);
            Quaternion upwardEnd = EulerAnglesConversion(-0.07f, 0.55f);

            switch (SwingCounter % 3)
            {
                // Swing the around in a forward motion, when it ending up behind the player at the end of the animation.
                case 0:
                    DoBehavior_SwingForward(forwardStart, forwardAnticipation, forwardSlash, forwardEnd);
                    break;

                // Swing the sword sharply up from the back position.
                case 1:
                    DoBehavior_SwingUpward(forwardEnd, upwardSlash, upwardEnd);
                    break;

                // Swing the sword around in a clean spin motion.
                case 2:
                    Behavior_SpinAround();
                    break;
            }
        }

        public void StickToOwner()
        {
            // Glue the sword to its owner.
            float rotationZAngle = Atan2((Rotation.W * Rotation.Z + Rotation.X * Rotation.Y) * 2f, 1f - (Rotation.Y.Squared() + Rotation.Z.Squared()) * 2f);
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir(AngleToXDirection(rotationZAngle));

            // Decide the arm rotation for the owner.
            float armRotation = rotationZAngle - (HorizontalDirection == 1f ? PiOver2 : Pi) - HorizontalDirection * PiOver4 + StartingRotation;
            Owner.SetCompositeArmFront(Math.Abs(armRotation) > 0.01f, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        public void DoBehavior_Vanish()
        {
            // Vanish.
            VanishTimer++;
            Projectile.scale = InverseLerp(11f, 0f, VanishTimer).Squared();

            // Die once completely shrunk.
            if (Projectile.scale <= 0f)
                Projectile.Kill();
        }

        public void DoBehavior_SwingForward(Quaternion forwardStart, Quaternion forwardAnticipation, Quaternion forwardSlash, Quaternion forwardEnd)
        {
            PiecewiseRotation rotationForward = new PiecewiseRotation().
                Add(PolynomialEasing.Quadratic, EasingType.Out, forwardAnticipation, 0.4f, forwardStart).
                Add(PolynomialEasing.Quartic, EasingType.In, forwardSlash, 0.6f).
                Add(PolynomialEasing.Quadratic, EasingType.Out, forwardEnd, 1f);
            Rotation = rotationForward.Evaluate(AnimationCompletion, HorizontalDirection == -1f && AnimationCompletion >= 0.6f, 1);

            // Shake the screen as the swing begins.
            if (Time == (int)(UseTime * 0.6f))
                StartShakeAtPoint(Projectile.Center, 2.8f);

            // Appear in the player's hand.
            Projectile.scale = InverseLerp(0f, 0.18f, AnimationCompletion);
        }

        public void DoBehavior_SwingUpward(Quaternion forwardEnd, Quaternion upwardSlash, Quaternion upwardEnd)
        {
            PiecewiseRotation rotationUpward = new PiecewiseRotation().
                Add(new PolynomialEasing(12f), EasingType.InOut, upwardSlash, 0.9f, forwardEnd).
                Add(PolynomialEasing.Quadratic, EasingType.In, upwardEnd, 1f);
            Rotation = rotationUpward.Evaluate(AnimationCompletion, AnimationCompletion < 0.85f, -1);

            // Shake the screen as the swing begins.
            if (Time == (int)(UseTime * 0.3f))
                StartShakeAtPoint(Projectile.Center, 2.8f);
        }

        public void Behavior_SpinAround()
        {
            float forwardAngle = Utils.MultiLerp(AnimationCompletion.Squared(), 0.55f, 0.87f, 0f);
            float spinAngle = Pi * PolynomialEasing.Cubic.Evaluate(EasingType.InOut, 1f - AnimationCompletion) * -4f;
            Rotation = EulerAnglesConversion(spinAngle, forwardAngle);

            // Shake the screen as the swing begins. This is slightly stronger than the older swings due to its longer execution.
            if (Time == (int)(UseTime * 0.25f))
                StartShakeAtPoint(Projectile.Center, 5f);

            // Disappear in the player's hand.
            Projectile.scale = InverseLerp(1f, 0.89f, AnimationCompletion);
        }

        private Quaternion EulerAnglesConversion(float angle2D, float angleSide = 0f)
        {
            float forwardRotationOffset = angle2D * HorizontalDirection + (HorizontalDirection == -1f ? PiOver2 : 0f);
            return Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(WrapAngle360(forwardRotationOffset)) * Matrix.CreateRotationX(angleSide));
        }

        // This projectile should remain glued to the owner's hand, and not move.
        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 quadArea = texture.Size();

            // Calculate matrices that define the quad's orientation.
            Matrix translation = Matrix.CreateTranslation(new Vector3(Projectile.Center.X - Main.screenPosition.X, Projectile.Center.Y - Main.screenPosition.Y, 0f));
            Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -100f, 100f);
            Matrix view = translation * Main.GameViewMatrix.TransformationMatrix * projection;
            Matrix rotation = Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateRotationZ(StartingRotation);
            Matrix scale = Matrix.CreateScale(Projectile.scale);

            // Generate the quads in a clockwise orientation.
            if (SwordQuad is null)
            {
                VertexPositionColorTexture topLeft = new(new(0f, -quadArea.Y, 0f), Color.White, Vector2.One * 0.01f);
                VertexPositionColorTexture topRight = new(new(quadArea.X, -quadArea.Y, 0f), Color.White, Vector2.UnitX * 0.99f);
                VertexPositionColorTexture bottomLeft = new(new(0f, 0f, 0f), Color.White, Vector2.UnitY * 0.99f);
                VertexPositionColorTexture bottomRight = new(new(quadArea.X, 0f, 0f), Color.White, Vector2.One * 0.99f);
                SwordQuad = [topLeft, topRight, bottomRight, bottomLeft];
            }

            // Draw the quads.
            ManagedShader projectionShader = ShaderManager.GetShader("PrimitiveProjectionShader");
            projectionShader.TrySetParameter("uWorldViewProjection", rotation * scale * view);
            projectionShader.Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            Main.instance.GraphicsDevice.Textures[1] = texture;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, SwordQuad, 0, SwordQuad.Length, QuadIndices, 0, QuadIndices.Length / 3);

            return false;
        }
    }
}
