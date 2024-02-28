using System;
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
            Projectile.MaxUpdates = 1;
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

            DoBehavior_SwingForward();

            // Decide the arm rotation for the owner.
            float rotationZAngle = Atan2((Rotation.W * Rotation.Z + Rotation.X * Rotation.Y) * 2f, 1f - (Rotation.Y.Squared() + Rotation.Z.Squared()) * 2f);
            float armRotation = rotationZAngle - (HorizontalDirection == 1f ? PiOver2 : 0f) - HorizontalDirection * PiOver4;
            Owner.SetCompositeArmFront(Math.Abs(armRotation) > 0.01f, Player.CompositeArmStretchAmount.Full, armRotation);

            // Glue the sword to its owner.
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir((int)HorizontalDirection);

            // Increment time and disappear once the AI timer has reached its maximum.
            Time++;
            if (Main.myPlayer == Projectile.owner && AnimationCompletion >= 1f)
            {
                // Check if the player is still using the sword. If they are, simply reset the AI timer.
                // Otherwise, die.
                if (Main.mouseLeft)
                {
                    Projectile.velocity = Projectile.DirectionToSafe(Main.MouseWorld);
                    StartingRotation = Projectile.velocity.ToRotation();
                    HorizontalDirection = Projectile.velocity.X.NonZeroSign();
                    SwingCounter++;
                    Time = 0f;
                    Projectile.netUpdate = true;
                }
                else
                    Projectile.Kill();
            }
        }

        public void DoBehavior_SwingForward()
        {
            if (Main.gameMenu)
                return;

            switch (SwingCounter % 1)
            {
                case 0:
                    Quaternion start = EulerAnglesConversion(-0.06f, 0.12f);
                    Quaternion anticipation = EulerAnglesConversion(-1.26f, 0.47f);
                    Quaternion slash = EulerAnglesConversion(2.65f, 0.8f);
                    Quaternion end = EulerAnglesConversion(2.65f, 0.15f);

                    PiecewiseRotation rotation = new PiecewiseRotation().
                        Add(PolynomialEasing.Quadratic, EasingType.Out, anticipation, 0.51f, start).
                        Add(PolynomialEasing.Quartic, EasingType.In, slash, 0.9f).
                        Add(PolynomialEasing.Quadratic, EasingType.Out, end, 1f);
                    Rotation = rotation.Evaluate(AnimationCompletion);

                    if (Time == (int)(UseTime * 0.7f))
                        StartShakeAtPoint(Projectile.Center, 3f);

                    Projectile.scale = InverseLerpBump(0f, 0.18f, 0.91f, 1f, AnimationCompletion);

                    break;
            }
        }

        /// <summary>
        /// Calculates a quaternion from a given 2D and 3D side angle, taking into account this sword's <see cref="StartingRotation"/>.
        /// </summary>
        /// <param name="angle2D">The 2D angle.</param>
        /// <param name="angleSide">The side angle.</param>
        private Quaternion EulerAnglesConversion(float angle2D, float angleSide = 0f)
        {
            float forwardRotationOffset = angle2D * HorizontalDirection + (HorizontalDirection == -1f ? PiOver2 : 0f);
            return Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(WrapAngle360(forwardRotationOffset)) * Matrix.CreateRotationX(angleSide));
        }

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
            VertexPositionColorTexture topLeft = new(new(0f, -quadArea.Y, 0f), Color.White, Vector2.One * 0.01f);
            VertexPositionColorTexture topRight = new(new(quadArea.X, -quadArea.Y, 0f), Color.White, Vector2.UnitX * 0.99f);
            VertexPositionColorTexture bottomLeft = new(new(0f, 0f, 0f), Color.White, Vector2.UnitY * 0.99f);
            VertexPositionColorTexture bottomRight = new(new(quadArea.X, 0f, 0f), Color.White, Vector2.One * 0.99f);
            VertexPositionColorTexture[] quad = [topLeft, topRight, bottomRight, bottomLeft];
            short[] quadIndices = [0, 1, 2, 2, 3, 0];

            // Draw the quads.
            ManagedShader projectionShader = ShaderManager.GetShader("PrimitiveProjectionShader");
            projectionShader.TrySetParameter("uWorldViewProjection", rotation * scale * view);
            projectionShader.Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            Main.instance.GraphicsDevice.Textures[1] = texture;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, quad, 0, quad.Length, quadIndices, 0, quadIndices.Length / 3);

            return false;
        }
    }
}
