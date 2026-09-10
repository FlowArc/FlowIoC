using UnityEngine;

namespace Modules.WorldPointerModule.Services.Sub
{
    /// <summary>
    /// The frame a pointer is judged against: the camera's pixel rect inset by the margins. Plain
    /// arithmetic on a Rect with no Unity object in it, so the corners, the margins and the ray
    /// can be tested without a camera.
    /// </summary>
    public readonly struct WorldPointerFrame
    {
        public readonly Rect Rect;

        public Vector2 Centre => Rect.center;

        public WorldPointerFrame(Rect pixelRect, RectOffset margins)
        {
            Rect = margins == null
                ? pixelRect
                : Rect.MinMaxRect(
                    pixelRect.xMin + margins.left,
                    pixelRect.yMin + margins.bottom,
                    pixelRect.xMax - margins.right,
                    pixelRect.yMax - margins.top);
        }

        public bool Contains(Vector2 point) => Rect.Contains(point);

        /// <summary>
        /// The point where the ray from the centre through <paramref name="point"/> leaves the
        /// frame, and that ray's direction. The centre is the frame's own rather than the
        /// screen's, so uneven margins need no special case: the half extents are symmetric
        /// about it. A point sitting on the centre has no direction, and goes up.
        /// </summary>
        public Vector2 ClampToEdge(Vector2 point, out Vector2 direction)
        {
            Vector2 centre = Rect.center;
            Vector2 offset = point - centre;

            if (offset.sqrMagnitude < 0.000001f)
            {
                direction = Vector2.up;
                return new Vector2(centre.x, Rect.yMax);
            }

            direction = offset.normalized;

            float halfWidth = Rect.width * 0.5f;
            float halfHeight = Rect.height * 0.5f;

            float alongX = Mathf.Abs(direction.x) > 0.000001f ? halfWidth / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float alongY = Mathf.Abs(direction.y) > 0.000001f ? halfHeight / Mathf.Abs(direction.y) : float.PositiveInfinity;

            return centre + direction * Mathf.Min(alongX, alongY);
        }
    }
}
