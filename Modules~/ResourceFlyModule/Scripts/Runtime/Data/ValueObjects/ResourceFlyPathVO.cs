using UnityEngine;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>One icon's way to its counter as a motion reads it this frame.</summary>
    public readonly struct ResourceFlyPathVO
    {
        /// <summary>Where the source was when the icon launched.</summary>
        public readonly Vector3 From;

        /// <summary>Where the target is this frame, so a counter that moves is homed in on.</summary>
        public readonly Vector3 To;

        /// <summary>
        /// Fixed for the icon's whole flight, so the Random methods answer the same every frame. A
        /// motion draws what it needs from them rather than from UnityEngine.Random, which would
        /// answer differently every frame and shake the icon.
        /// </summary>
        public readonly int Seed;

        /// <summary>The icons' canvas scale, so distances authored in canvas units hold at any resolution.</summary>
        public readonly float Scale;

        public ResourceFlyPathVO(Vector3 from, Vector3 to, int seed, float scale)
        {
            From = from;
            To = to;
            Seed = seed;
            Scale = scale;
        }

        /// <summary>
        /// A number in [0, 1) for this flight. Each stream is a number of its own: give every
        /// random thing a motion picks a different stream, or two of them move together.
        /// </summary>
        public float RandomValue(int stream)
        {
            unchecked
            {
                // Murmur3's finaliser over the seed and the stream: no allocation, the same answer every frame.
                uint hash = (uint) Seed * 0x9E3779B1u ^ (uint) stream * 0x85EBCA77u;
                hash ^= hash >> 16;
                hash *= 0x85EBCA6Bu;
                hash ^= hash >> 13;
                hash *= 0xC2B2AE35u;
                hash ^= hash >> 16;
                return (hash >> 8) * (1f / 16777216f);
            }
        }

        /// <summary>A point spread evenly over the unit disc for this flight. Takes this stream and the next.</summary>
        public Vector2 RandomInsideUnitCircle(int stream) =>
            RandomOnUnitCircle(stream) * Mathf.Sqrt(RandomValue(stream + 1));

        /// <summary>A point on the unit circle for this flight.</summary>
        public Vector2 RandomOnUnitCircle(int stream)
        {
            float angle = RandomValue(stream) * 2f * Mathf.PI;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}