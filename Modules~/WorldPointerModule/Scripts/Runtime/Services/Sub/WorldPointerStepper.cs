using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Enums;
using UnityEngine;

namespace Modules.WorldPointerModule.Services.Sub
{
    /// <summary>
    /// One target's frame: projected through the camera, judged against its display's frame,
    /// placed on its indicator's parent plane and, on the edge, its arrow aimed. It answers the
    /// state and leaves telling the indicator to the service, which knows whether it changed.
    /// </summary>
    internal sealed class WorldPointerStepper
    {
        private readonly Vector3[] _corners = new Vector3[4];

        /// <summary>
        /// Behind the camera Unity's projection is mirrored through the centre, so it is mirrored
        /// back before anything reads it. A clamped indicator is judged against the frame shrunk by
        /// its own half size on screen, so the whole of it stays visible at the edge rather than
        /// half of it hanging off.
        /// </summary>
        public WorldPointerState Step(WorldPointerTargetRVO entry, WorldPointerOptionsCVO options, Camera camera, float deltaTime)
        {
            Vector3 screen = camera.WorldToScreenPoint(entry.Target.position + options.WorldOffset);
            bool behind = screen.z < 0f;
            Vector2 point = (Vector2) screen + options.ScreenOffset;

            var frame = new WorldPointerFrame(camera.pixelRect, options.ScreenMargins);
            if (options.OffScreen == OffScreenMode.ClampToEdge) frame = frame.Inset(HalfSizeOnScreen(entry));
            if (behind) point = frame.Centre - (point - frame.Centre);

            bool inside = !behind && frame.Contains(point);

            switch (options.OffScreen)
            {
                case OffScreenMode.Hide:
                    if (!inside) return WorldPointerState.Hidden;

                    Place(entry, point, options.SmoothSpeed, deltaTime);
                    return WorldPointerState.InFrame;

                case OffScreenMode.ClampToEdge:
                    WorldPointerState state = WorldPointerState.InFrame;

                    if (!inside)
                    {
                        state = WorldPointerState.OnEdge;
                        point = frame.ClampToEdge(point, out Vector2 direction);
                        if (options.RotateArrow) AimArrow(entry, direction, options.SmoothSpeed, deltaTime);
                    }

                    Place(entry, point, options.SmoothSpeed, deltaTime);
                    return state;

                default:
                    Place(entry, point, options.SmoothSpeed, deltaTime);
                    return WorldPointerState.InFrame;
            }
        }

        /// <summary>
        /// Half the indicator's width and height in screen pixels, from its corners - so a canvas
        /// scaler, a scaled parent and both canvas modes all come out right.
        /// </summary>
        private Vector2 HalfSizeOnScreen(WorldPointerTargetRVO entry)
        {
            entry.Indicator.Rect.GetWorldCorners(_corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(entry.CanvasCamera, _corners[0]);
            Vector2 max = min;

            for (int i = 1; i < _corners.Length; i++)
            {
                Vector2 corner = RectTransformUtility.WorldToScreenPoint(entry.CanvasCamera, _corners[i]);
                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }

            return (max - min) * 0.5f;
        }

        /// <summary>
        /// Local up along the direction: atan2 gives the angle of +x, and up is a quarter turn on.
        /// A local rotation is right in both canvas modes, where a world-space up is not.
        /// </summary>
        private static void AimArrow(WorldPointerTargetRVO entry, Vector2 direction, float speed, float deltaTime)
        {
            RectTransform pivot = entry.Indicator.ArrowPivot;
            if (pivot == null) return;

            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);

            pivot.localRotation = speed > 0f
                ? Quaternion.Slerp(pivot.localRotation, rotation, 1f - Mathf.Exp(-speed * deltaTime))
                : rotation;
        }

        /// <summary>
        /// By world point on the parent's plane rather than by anchoredPosition against the root,
        /// which is what makes any parent and any anchors correct in both canvas modes.
        /// </summary>
        private static void Place(WorldPointerTargetRVO entry, Vector2 point, float speed, float deltaTime)
        {
            RectTransform rect = entry.Indicator.Rect;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, point, entry.CanvasCamera, out Vector3 world))
                return;

            rect.position = speed > 0f
                ? Vector3.Lerp(rect.position, world, 1f - Mathf.Exp(-speed * deltaTime))
                : world;
        }
    }
}
