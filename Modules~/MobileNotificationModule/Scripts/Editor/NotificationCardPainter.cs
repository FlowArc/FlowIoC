#if UNITY_EDITOR

using Modules.MobileNotificationModule.Data.ValueObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.MobileNotificationModule.Editor
{
    /// <summary>
    /// Draws a notification the way the two trays draw it, inside a rect the panel hands over:
    /// Android's dark card - the small icon tinted in the app's colour, the app name and the time
    /// over a bold title and a two-line body, the large icon on the right, and the picture under
    /// the text when the template names one, as the expanded notification shows it - and iOS's
    /// light card with the app icon, the app name in capitals, the title and the body, the
    /// picture as the thumbnail at the right. A likeness, not a screenshot: the phone's font,
    /// radius and spacing differ by version and vendor, but what a developer checks here - does
    /// the title fit, does the body cut where they expect, is the icon the right one - reads the
    /// same.
    /// </summary>
    internal class NotificationCardPainter
    {
        private const float ANDROID_HEIGHT = 124f;
        private const float ANDROID_PICTURE_HEIGHT = 150f;
        private const float IOS_HEIGHT = 108f;

        private const float MAX_WIDTH = 460f;
        private const float MARGIN = 6f;
        private const float RADIUS = 18f;
        private const float PADDING = 16f;
        private const int BODY_LINES = 2;

        private static readonly Color AndroidCard = new(0.11f, 0.11f, 0.125f);
        private static readonly Color AndroidMeta = new(0.72f, 0.72f, 0.74f);
        private static readonly Color AndroidBody = new(0.80f, 0.80f, 0.82f);
        private static readonly Color IosCard = new(0.94f, 0.94f, 0.95f);
        private static readonly Color IosMeta = new(0.45f, 0.45f, 0.47f);
        private static readonly Color IosTitle = new(0.08f, 0.08f, 0.09f);
        private static readonly Color IosBody = new(0.30f, 0.30f, 0.32f);

        private GUIStyle _meta;
        private GUIStyle _title;
        private GUIStyle _body;

        /// <summary>The row height the Android card needs: taller when a picture opens under the text.</summary>
        public float AndroidHeight(NotificationCardEVO card) =>
            ANDROID_HEIGHT + (card.Picture != null ? ANDROID_PICTURE_HEIGHT : 0f);

        public float IosHeight(NotificationCardEVO card) => IOS_HEIGHT;

        public void Android(Rect content, NotificationCardEVO card)
        {
            Rect box = Box(content, AndroidHeight(card));
            Rounded(box, AndroidCard);

            float x = box.x + PADDING;
            float y = box.y + 12f;
            float right = box.xMax - PADDING;

            // The large icon takes the right edge when there is one; the texts stop short of it.
            if (card.LargeIcon != null)
            {
                var large = new Rect(right - 48f, box.y + (ANDROID_HEIGHT - MARGIN * 2f - 48f) * 0.5f, 48f, 48f);
                GUI.DrawTexture(large, card.LargeIcon, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 8f);
                right = large.x - 12f;
            }

            // The small icon is an alpha mask the OS colours; a missing one is the app icon.
            var small = new Rect(x, y, 16f, 16f);

            if (card.SmallIcon != null)
                GUI.DrawTexture(small, card.SmallIcon, ScaleMode.ScaleToFit, true, 0f, card.Accent, 0f, 0f);
            else if (card.AppIcon != null)
                GUI.DrawTexture(small, card.AppIcon, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 4f);

            GUIStyle meta = Meta(AndroidMeta);
            string header = card.AppName + "  •  " + card.Time;
            GUI.Label(new Rect(x + 24f, y - 2f, right - x - 24f, 18f), header, meta);

            y += 24f;
            GUI.Label(new Rect(x, y, right - x, 20f), Fit(Title(Color.white), card.Title, right - x, 1), Title(Color.white));

            y += 22f;
            GUIStyle body = Body(AndroidBody);
            GUI.Label(new Rect(x, y, right - x, body.lineHeight * BODY_LINES + 2f), Fit(body, card.Body, right - x, BODY_LINES), body);

            // Expanded, the big picture spans the card under the text, the way BigPictureStyle lays it out.
            if (card.Picture != null)
            {
                var picture = new Rect(x, box.y + ANDROID_HEIGHT - MARGIN * 2f, box.xMax - PADDING - x, ANDROID_PICTURE_HEIGHT - 12f);
                GUI.DrawTexture(picture, card.Picture, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, 10f);
            }
        }

        public void Ios(Rect content, NotificationCardEVO card)
        {
            Rect box = Box(content, IosHeight(card));
            Rounded(box, IosCard);

            float x = box.x + PADDING;
            float y = box.y + 14f;
            float right = box.xMax - PADDING;

            var icon = new Rect(x, y, 38f, 38f);

            if (card.AppIcon != null)
                GUI.DrawTexture(icon, card.AppIcon, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 9f);
            else
                Rounded(icon, IosMeta, 9f);

            // The attachment is the thumbnail at the right of the banner; the texts stop short of it.
            if (card.Picture != null)
            {
                var thumbnail = new Rect(right - 56f, box.y + (box.height - 56f) * 0.5f, 56f, 56f);
                GUI.DrawTexture(thumbnail, card.Picture, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, 8f);
                right = thumbnail.x - 12f;
            }

            float textX = icon.xMax + 12f;
            GUIStyle meta = Meta(IosMeta);
            GUI.Label(new Rect(textX, y - 2f, right - textX - 40f, 18f), card.AppName.ToUpperInvariant(), meta);

            GUIStyle time = Meta(IosMeta);
            time.alignment = TextAnchor.UpperRight;
            GUI.Label(new Rect(right - 60f, y - 2f, 60f, 18f), card.Time, time);

            y += 18f;
            GUI.Label(new Rect(textX, y, right - textX, 20f), Fit(Title(IosTitle), card.Title, right - textX, 1), Title(IosTitle));

            y += 21f;
            GUIStyle body = Body(IosBody);
            GUI.Label(new Rect(textX, y, right - textX, body.lineHeight * BODY_LINES + 2f), Fit(body, card.Body, right - textX, BODY_LINES), body);
        }

        private Rect Box(Rect content, float height) =>
            new(content.x, content.y + MARGIN, Mathf.Min(content.width, MAX_WIDTH), height - MARGIN * 2f);

        private void Rounded(Rect rect, Color color, float radius = RADIUS) =>
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);

        /// <summary>
        /// The text cut to the lines the tray shows, with an ellipsis where it was cut, so a body
        /// too long for the collapsed card is seen to be too long here.
        /// </summary>
        private string Fit(GUIStyle style, string text, float width, int lines)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            float allowed = style.lineHeight * lines + 1f;

            if (style.CalcHeight(new GUIContent(text), width) <= allowed)
                return text;

            int length = text.Length;

            while (length > 0)
            {
                length--;
                string candidate = text.Substring(0, length).TrimEnd() + "…";

                if (style.CalcHeight(new GUIContent(candidate), width) <= allowed)
                    return candidate;
            }

            return "…";
        }

        private GUIStyle Meta(Color color)
        {
            _meta ??= new GUIStyle(EditorStyles.label) {fontSize = 11, clipping = TextClipping.Clip, wordWrap = false};
            _meta.normal.textColor = color;
            _meta.alignment = TextAnchor.UpperLeft;
            return _meta;
        }

        private GUIStyle Title(Color color)
        {
            _title ??= new GUIStyle(EditorStyles.boldLabel) {fontSize = 13, clipping = TextClipping.Clip, wordWrap = false};
            _title.normal.textColor = color;
            return _title;
        }

        private GUIStyle Body(Color color)
        {
            _body ??= new GUIStyle(EditorStyles.label) {fontSize = 12, wordWrap = true, clipping = TextClipping.Clip};
            _body.normal.textColor = color;
            return _body;
        }
    }
}

#endif