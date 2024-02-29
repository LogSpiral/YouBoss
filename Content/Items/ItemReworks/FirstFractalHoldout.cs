using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using YouBoss.Assets;
using YouBoss.Common.Tools.Easings;
using YouBoss.Core.Graphics.Automators;
using YouBoss.Core.Graphics.Primitives;
using YouBoss.Core.Graphics.Shaders;
using YouBoss.Core.Graphics.SpecificEffectManagers;
using static YouBoss.Content.Items.ItemReworks.FirstFractal;

namespace YouBoss.Content.Items.ItemReworks
{
    public class FirstFractalHoldout : ModProjectile, IDrawLocalDistortion
    {
        private List<Vector2> trailPositions;

        private short[] trailIndices;

        private VertexPositionColorTexture[] trailVertices;

        private Matrix compositeVertexMatrix;

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
        /// How much longer the anime hit visuals will go on for, in frames.
        /// </summary>
        public int AnimeHitVisualsCountdown
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
        /// Whether the player is currently dashing or not.
        /// </summary>
        public bool OwnerIsDashing
        {
            get;
            set;
        }

        /// <summary>
        /// Whether owner direction changing should be disabled or not.
        /// </summary>
        public bool DontChangeOwnerDirection
        {
            get;
            set;
        }

        /// <summary>
        /// The previous starting rotation of the blade prior to the current animation sequence.
        /// </summary>
        public float OldStartingRotation
        {
            get;
            set;
        }

        /// <summary>
        /// The Z axis' rotation relative to the <see cref="Rotation"/> of this sword.
        /// </summary>
        public float ZRotation => Atan2((Rotation.W * Rotation.Z + Rotation.X * Rotation.Y) * 2f, 1f - (Rotation.Y.Squared() + Rotation.Z.Squared()) * 2f);

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
        /// The amount of updates this sword performs each frame. Higher values for this are useful because they allow for finer subdivisions of the swing animations, thus making the rotation changes less sudden each frame.
        /// </summary>
        public static int MaxUpdates => 3;

        /// <summary>
        /// How long the enemy on-hit anime visuals effect lasts.
        /// </summary>
        public static int AnimeVisualsDuration => SecondsToFrames(0.05f);

        /// <summary>
        /// The base scale of this sword.
        /// </summary>
        public static float BaseScale => 1.2f;

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

        public override string Texture => "YouBoss/Content/Items/ItemReworks/FirstFractal";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 120;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 720000;
            Projectile.MaxUpdates = MaxUpdates;
            Projectile.noEnchantmentVisuals = true;

            // Use local i-frames.
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = MaxUpdates * 3;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(OwnerIsDashing);

            writer.Write(VanishTimer);
            writer.Write(SwingCounter);

            writer.Write(OldStartingRotation);
            writer.Write(Projectile.rotation);

            writer.Write(Rotation.X);
            writer.Write(Rotation.Y);
            writer.Write(Rotation.Z);
            writer.Write(Rotation.W);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OwnerIsDashing = reader.ReadBoolean();

            VanishTimer = reader.ReadInt32();
            SwingCounter = reader.ReadInt32();

            OldStartingRotation = reader.ReadSingle();
            Projectile.rotation = reader.ReadSingle();

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();
            Rotation = new(x, y, z, w);
        }

        public override void AI()
        {
            // Initialize things.
            if (HorizontalDirection == 0f)
            {
                StartingRotation = Projectile.velocity.ToRotation();
                HorizontalDirection = Projectile.velocity.X.NonZeroSign();
                Time = 0f;
                Projectile.netUpdate = true;
            }

            // Stick to the owner.
            StickToOwner();

            // Reset things every frame.
            DontChangeOwnerDirection = false;
            OwnerIsDashing = false;

            // Vanish if necessary.
            if (VanishTimer >= 1)
            {
                DoBehavior_Vanish();
                return;
            }

            // Handle anime hit effect visuals.
            if (AnimeHitVisualsCountdown > 0)
            {
                Owner.velocity = Vector2.UnitX * HorizontalDirection * PlayerPostHitSpeed;
                Time = (int)(UseTime * 0.94f);
                AnimeHitVisualsCountdown--;
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
                        HorizontalDirection = Projectile.velocity.X.NonZeroSign();
                        OldStartingRotation = StartingRotation;
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

            // Store the rotation.
            Projectile.rotation = StartingRotation + ZRotation;
        }

        public void HandleSlashes()
        {
            // Calculate rotation keyframes in advance.
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
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            if (!DontChangeOwnerDirection && Time >= 2 && VanishTimer <= 0)
                Owner.ChangeDir(AngleToXDirection(ZRotation) * (int)HorizontalDirection);

            // Decide the arm rotation for the owner.
            float armRotation = ZRotation - (HorizontalDirection == 1f ? PiOver2 : Pi) - HorizontalDirection * PiOver4 + StartingRotation;
            Owner.SetCompositeArmFront(Math.Abs(armRotation) > 0.01f, Player.CompositeArmStretchAmount.Full, armRotation);

            // Create slash particles.
            CreateSlashParticles();
        }

        public void CreateSlashParticles()
        {
            float angularOffset = WrapAngle(Projectile.rotation - Projectile.oldRot[1]);
            float angularVelocity = Abs(angularOffset);
            if (angularVelocity <= 0.1f || Projectile.scale <= 0.5f)
                return;

            Vector2 forward = (Projectile.rotation - PiOver4).ToRotationVector2();
            Vector2 perpendicular = forward.RotatedBy(angularOffset.NonZeroSign() * PiOver2);
            Dust energy = Dust.NewDustPerfect(Projectile.Center + forward * Main.rand.NextFloat(28f, 74f) * Projectile.scale, 264);
            energy.velocity = perpendicular * 3f + Owner.velocity * 0.35f;
            energy.color = Color.Lerp(Color.Turquoise, Color.Yellow, Sqrt(Main.rand.NextFloat(0.95f)));
            energy.scale = Main.rand.NextFloat(1f, 1.6f);
            energy.fadeIn = Main.rand.NextFloat(0.9f);
            energy.noGravity = true;
        }

        public void DoBehavior_Vanish()
        {
            // Vanish.
            if (Projectile.IsFinalExtraUpdate())
                VanishTimer++;

            Projectile.scale = InverseLerp(11f, 0f, VanishTimer).Squared() * BaseScale;

            // Die once completely shrunk.
            if (Projectile.scale <= 0f)
                Projectile.Kill();
        }

        public void DoBehavior_SwingForward(Quaternion forwardStart, Quaternion forwardAnticipation, Quaternion forwardSlash, Quaternion forwardEnd)
        {
            // Look towards the mouse.
            if (Main.myPlayer == Projectile.owner && Time < MaxUpdates * 10f && SwingCounter >= 1)
            {
                float baseRotation = Projectile.AngleTo(Main.MouseWorld);
                if (Sin(baseRotation) > Sin(OldStartingRotation))
                    baseRotation += Pi;

                StartingRotation = StartingRotation.AngleLerp(baseRotation, Time / MaxUpdates / 35f);
            }

            bool longerAnticipation = SwingCounter == 0;
            PiecewiseRotation rotationForward = new PiecewiseRotation().
                Add(SineEasing.Default, longerAnticipation ? EasingType.Out : EasingType.In, forwardAnticipation, 0.5f, forwardStart).
                Add(PolynomialEasing.Quartic, EasingType.In, forwardSlash, 0.7f).
                Add(PolynomialEasing.Quadratic, EasingType.Out, forwardEnd, 1f);
            Rotation = rotationForward.Evaluate(AnimationCompletion, HorizontalDirection == -1f && AnimationCompletion >= 0.7f, 1);

            // Ensure that the player's pose doesn't get changed during the anticipation.
            DontChangeOwnerDirection = true;

            // Shake the screen as the swing begins.
            if (Time == (int)(UseTime * 0.7f))
            {
                Owner.velocity.X = HorizontalDirection * PlayerHorizontalDashSpeed;
                Owner.velocity.Y *= Exp(PlayerHorizontalDashSpeed * -0.0145f);

                StartShakeAtPoint(Projectile.Center, 2.8f);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound with { Pitch = Main.rand.NextFloat(0.06f) }, Projectile.Center);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SlashSound, Projectile.Center);
            }

            OwnerIsDashing = AnimationCompletion >= 0.7f && AnimationCompletion < 0.95f;
            if (OwnerIsDashing && Owner.immuneTime <= 1)
                Owner.SetImmuneTimeForAllTypes(2);

            // Slow the player down after the dash.
            if (Time == (int)(UseTime * 0.95f) && AnimeHitVisualsCountdown <= 0)
                Owner.velocity.X *= 0.17f;

            // Appear in the player's hand.
            if (SwingCounter <= 0)
                Projectile.scale = InverseLerp(0f, 0.18f, AnimationCompletion) * BaseScale;
        }

        public void DoBehavior_SwingUpward(Quaternion forwardEnd, Quaternion upwardSlash, Quaternion upwardEnd)
        {
            PiecewiseRotation rotationUpward = new PiecewiseRotation().
                Add(new PolynomialEasing(12f), EasingType.InOut, upwardSlash, 0.9f, forwardEnd).
                Add(PolynomialEasing.Quadratic, EasingType.In, upwardEnd, 1f);
            Rotation = rotationUpward.Evaluate(AnimationCompletion, AnimationCompletion < 0.85f, -1);

            // Ensure that the player's pose doesn't get changed during the anticipation.
            DontChangeOwnerDirection = true;

            // Shake the screen as the swing begins.
            if (Time == (int)(UseTime * 0.3f))
            {
                StartShakeAtPoint(Projectile.Center, 2.8f);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.DashSound with { Pitch = Main.rand.NextFloat(0.06f) }, Projectile.Center);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SlashSound, Projectile.Center);
            }
        }

        public void Behavior_SpinAround()
        {
            float forwardAngle = Utils.MultiLerp(AnimationCompletion.Squared(), 0.55f, 1.16f, 0f);
            float spinAngle = Pi * new PolynomialEasing(3.5f).Evaluate(EasingType.InOut, 1f - AnimationCompletion) * -4f;
            Rotation = EulerAnglesConversion(spinAngle, forwardAngle);

            // Shake the screen as the swing begins. This is slightly stronger than the older swings due to its longer execution.
            if (Time == (int)(UseTime * 0.25f))
            {
                StartShakeAtPoint(Projectile.Center, 5f);
                SoundEngine.PlaySound(SoundsRegistry.TerraBlade.SplitSound, Projectile.Center);
            }

            // Create homing beams.
            if (Main.myPlayer == Projectile.owner && AnimationCompletion >= 0.25f && Time % 3f == 0f)
            {
                Vector2 beamVelocity = (TwoPi * InverseLerp(0.25f, 1f, AnimationCompletion) + StartingRotation - PiOver4).ToRotationVector2() * new Vector2(1f, 0.4f) * HomingBeamStartingSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, beamVelocity.RotatedBy(StartingRotation), ModContent.ProjectileType<HomingTerraBeam>(), (int)(Projectile.damage * HomingBeamDamageFactor), 0f, Projectile.owner);
            }
        }

        private Quaternion EulerAnglesConversion(float angle2D, float angleSide = 0f)
        {
            float forwardRotationOffset = angle2D * HorizontalDirection + (HorizontalDirection == -1f ? PiOver2 : 0f);
            return Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(WrapAngle360(forwardRotationOffset)) * Matrix.CreateRotationX(angleSide));
        }

        // This projectile should remain glued to the owner's hand, and not move.
        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 swordDirection = (Projectile.rotation - PiOver4).ToRotationVector2();
            Vector2 start = Projectile.Center;
            Vector2 end = start + swordDirection * Projectile.scale * 112f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width * 0.5f, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 quadArea = texture.Size();

            // Calculate matrices that define the quad's orientation.
            Matrix translation = Matrix.CreateTranslation(new Vector3(Projectile.Center.X - Main.screenPosition.X, Projectile.Center.Y - Main.screenPosition.Y, 0f));
            Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -150f, 150f);
            Matrix view = translation * Main.GameViewMatrix.TransformationMatrix * projection;
            Matrix rotation = Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateRotationZ(StartingRotation);
            Matrix scale = Matrix.CreateScale(Projectile.scale);
            compositeVertexMatrix = rotation * scale * view;

            // Generate the quads in a clockwise orientation.
            if (SwordQuad is null)
            {
                VertexPositionColorTexture topLeft = new(new(0f, -quadArea.Y, 0f), Color.White, Vector2.One * 0.01f);
                VertexPositionColorTexture topRight = new(new(quadArea.X, -quadArea.Y, 0f), Color.White, Vector2.UnitX * 0.99f);
                VertexPositionColorTexture bottomLeft = new(new(0f, 0f, 0f), Color.White, Vector2.UnitY * 0.99f);
                VertexPositionColorTexture bottomRight = new(new(quadArea.X, 0f, 0f), Color.White, Vector2.One * 0.99f);
                SwordQuad = [topLeft, topRight, bottomRight, bottomLeft];
            }

            // Draw the afterimage trail.
            DefineAfterimageTrailCache();
            DrawAfterimageTrail();

            // Draw the sword.
            ManagedShader projectionShader = ShaderManager.GetShader("PrimitiveProjectionShader");
            projectionShader.TrySetParameter("uWorldViewProjection", compositeVertexMatrix);
            projectionShader.Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            Main.instance.GraphicsDevice.Textures[1] = texture;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, SwordQuad, 0, SwordQuad.Length, QuadIndices, 0, QuadIndices.Length / 3);

            return false;
        }

        public void DefineAfterimageTrailCache()
        {
            // Prepare the list of smoothened positions.
            int oldPositionCount = 20;
            int subdivisions = 5;
            float afterimageOffset = 118f;
            trailPositions = [];
            for (int i = 0; i < oldPositionCount; i++)
            {
                float startingRotation = Projectile.oldRot[i] - Projectile.rotation - PiOver4;
                float endingRotation = Projectile.oldRot[i + 1] - Projectile.rotation - PiOver4;
                for (int j = 0; j < subdivisions; j++)
                {
                    float rotation = startingRotation.AngleLerp(endingRotation, j / (float)subdivisions);
                    trailPositions.Add(Projectile.Center + rotation.ToRotationVector2() * afterimageOffset);
                }
            }

            // Terminate this method immediately if there's no trail points to use.
            if (!trailPositions.Any())
                return;

            // Prepare the trail vertex cache.
            float angularOffset = WrapAngle(Projectile.rotation - Projectile.oldRot[1]);
            float angularVelocity = Abs(angularOffset);
            float afterimageOpacity = InverseLerp(0.056f, 0.1f, angularVelocity);
            trailVertices = new VertexPositionColorTexture[trailPositions.Count * 2];
            trailIndices = PrimitiveTrail.GetIndicesFromTrailPoints(trailPositions.Count);
            for (int i = 0; i < trailPositions.Count; i++)
            {
                float trailCompletionRatio = i / (float)(trailPositions.Count - 1f);
                Vector2 leftTextureCoords = new(trailCompletionRatio, 0f);
                Vector2 rightTextureCoords = new(trailCompletionRatio, 1f);

                Vector2 forwardDirection = (trailPositions[i] - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Vector3 leftPosition = new(trailPositions[i] - Projectile.Center, 0f);
                Vector3 rightPosition = new(trailPositions[i] - Projectile.Center - forwardDirection * Projectile.scale * 90f, 0f);

                Color c = Color.White * afterimageOpacity;
                trailVertices[i * 2] = new(leftPosition, c, leftTextureCoords);
                trailVertices[i * 2 + 1] = new(rightPosition, c, rightTextureCoords);
            }
        }

        public void DrawAfterimageTrail()
        {
            // Prepare the trail shader.
            ManagedShader trailShader = ShaderManager.GetShader("FirstFractalTrailShader");
            trailShader.SetTexture(WavyBlotchNoise, 1, SamplerState.LinearWrap);
            trailShader.TrySetParameter("colorA", new Vector3(0.1f, 0.64f, 0.82f));
            trailShader.TrySetParameter("colorB", new Vector3(1f, 1f, 0.05f));
            trailShader.TrySetParameter("blackAppearanceInterpolant", 0.36f);
            trailShader.TrySetParameter("trailAnimationSpeed", 1.2f);
            trailShader.TrySetParameter("flatOpacity", false);
            trailShader.TrySetParameter("uWorldViewProjection", compositeVertexMatrix);
            trailShader.Apply();

            // Draw the trail.
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, trailVertices, 0, trailVertices.Length, trailIndices, 0, trailIndices.Length / 3);
        }

        public void DrawLocalDistortion(SpriteBatch spriteBatch)
        {
            if (trailVertices is null)
                return;

            float angularOffset = WrapAngle(Projectile.rotation - Projectile.oldRot[1]);
            float angularVelocity = Abs(angularOffset);
            float baseDistortion = InverseLerp(0.04f, 0.09f, angularVelocity) * 0.156f;
            VertexPositionColorTexture[] distortionVertices = new VertexPositionColorTexture[trailVertices.Length];
            for (int i = 0; i < distortionVertices.Length; i++)
            {
                Vector2 distortionPosition = trailPositions[i / 2];
                Vector2 offsetFromCenter = distortionPosition - Projectile.Center;
                float distortionAngle = offsetFromCenter.ToRotation() - ZRotation;
                float distortionX = Cos01(distortionAngle);
                float distortionY = Sin01(distortionAngle);

                distortionVertices[i] = trailVertices[i];
                distortionVertices[i].Color = new(distortionX, distortionY, (1f - i % 2) * InverseLerp(8f, 32f, i).Squared() * baseDistortion);
            }

            // Prepare the trail shader.
            ManagedShader trailShader = ShaderManager.GetShader("FirstFractalTrailShader");
            trailShader.SetTexture(WavyBlotchNoise, 1, SamplerState.LinearWrap);
            trailShader.TrySetParameter("colorA", new Vector3(1f, 1f, 1f));
            trailShader.TrySetParameter("colorB", new Vector3(1f, 1f, 1f));
            trailShader.TrySetParameter("blackAppearanceInterpolant", -3f);
            trailShader.TrySetParameter("trailAnimationSpeed", 0.6f);
            trailShader.TrySetParameter("flatOpacity", false);
            trailShader.TrySetParameter("uWorldViewProjection", compositeVertexMatrix);
            trailShader.Apply();

            // Draw the trail.
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, distortionVertices, 0, distortionVertices.Length, trailIndices, 0, trailIndices.Length / 3);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (OwnerIsDashing && AnimeHitVisualsCountdown <= 0)
            {
                if (TerraBladeSilhouetteDrawSystem.SilhouetteOpacity <= 0f)
                    TerraBladeSilhouetteDrawSystem.Subject = target;

                AnimeHitVisualsCountdown = AnimeVisualsDuration;
                StartShakeAtPoint(target.Center, 6.4f);
                Owner.SetImmuneTimeForAllTypes(PlayerPostHitIFrameGracePeriod);
            }
        }
    }
}
