using System;
using Modules.WorldPointerModule.Enums;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.ValueObjects
{
    /// <summary>
    /// How one pointer behaves. Six fields, each with one job; a null options argument to
    /// Register means a fresh one of these - no offsets, Hide, no margins, snap.
    /// </summary>
    [Serializable]
    public class WorldPointerOptionsCVO
    {
        [Tooltip("Added to the target's position before projecting - the height of a head.")]
        public Vector3 WorldOffset;

        [Tooltip("Screen pixels added after projecting - clear of the thing being pointed at.")]
        public Vector2 ScreenOffset;

        public OffScreenMode OffScreen = OffScreenMode.Hide;

        [Tooltip("The frame, as insets from the camera's pixel rect. A HUD bar across the top is a top inset of its height: a target under the bar is off screen, and a clamped pointer stops below it.")]
        public RectOffset ScreenMargins = new RectOffset();

        [Tooltip("ClampToEdge only. The arrow pivot's up is aimed at the target while on the edge.")]
        public bool RotateArrow;

        [Tooltip("0 snaps. Above 0 the pointer eases towards its place by 1 - exp(-speed * dt).")]
        public float SmoothSpeed;
    }
}
