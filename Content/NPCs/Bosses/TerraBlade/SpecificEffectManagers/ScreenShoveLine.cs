using System;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class ScreenShoveLine(int lifetime, Vector2 direction, Vector2 origin, Func<float, float> widthFunction)
    {
        /// <summary>
        /// How long this line has existed so far, in frames.
        /// </summary>
        public int Time;

        /// <summary>
        /// How long this line should exist for, in frames.
        /// </summary>
        public int Lifetime = lifetime;

        /// <summary>
        /// The width function that calculates how wide the line is.
        /// </summary>
        public Func<float, float> WidthFunction = widthFunction;

        /// <summary>
        /// This line's direction.
        /// </summary>
        public Vector2 Direction = direction;

        /// <summary>
        /// This line's origin.
        /// </summary>
        public Vector2 Origin = origin;

        /// <summary>
        /// Whether this line is currently active.
        /// </summary>
        public bool Active => Time < Lifetime;

        public void Update()
        {
            Time++;
        }
    }
}
