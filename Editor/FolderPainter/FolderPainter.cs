#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.FolderPainter
{
    /// <summary>
    /// Paints the Project window folder rows from the project local config.
    /// <see cref="FolderPainterBootstrap"/> owns the instance.
    /// </summary>
    public class FolderPainter
    {
        private readonly FolderPainterConfigLoader _configLoader = new FolderPainterConfigLoader();
        private readonly Dictionary<string, FolderPainterFolderRuleEVO> _folderRuleMap = new Dictionary<string, FolderPainterFolderRuleEVO>();

        private ED_FolderPainter _config;

        internal string ConfigPath => _configLoader.ConfigPath;

        internal ED_FolderPainter EnsureConfig()
        {
            return _configLoader.EnsureConfig();
        }

        /// <summary>
        /// Rereads the config and rebuilds the folder lookup. Safe to call repeatedly.
        /// </summary>
        public void Apply()
        {
            _config = _configLoader.Load();
            _folderRuleMap.Clear();
            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;

            if (_config == null || !_config.Enabled) return;

            if (_config.FolderRules != null)
            {
                foreach (FolderPainterFolderRuleEVO folderColorRule in _config.FolderRules)
                {
                    string path = folderColorRule?.FolderRule?.Path;
                    if (string.IsNullOrEmpty(path)) continue;

                    _folderRuleMap[path] = folderColorRule;
                }
            }

            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        private void OnProjectWindowItemGUI(string guid, Rect rect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path)) return;

            if (_folderRuleMap.TryGetValue(path, out FolderPainterFolderRuleEVO rule))
            {
                DrawWithVisualConfig(rule.VisualConfig, rect, guid, path);
            }
            else
            {
                ResolvePathRule(rect, guid, path);
            }
        }

        private void ResolvePathRule(Rect rect, string guid, string path)
        {
            if (_config.PathRules == null) return;

            foreach (FolderPainterPathRuleEVO rule in _config.PathRules)
            {
                if (rule?.PathRule == null || string.IsNullOrEmpty(rule.PathRule.Value)) continue;

                if (rule.PathRule.Type == FolderPainterPathCheckType.Contains && path.Contains(rule.PathRule.Value))
                {
                    DrawWithVisualConfig(rule.Visual, rect, guid, path);
                    return;
                }
                else if (rule.PathRule.Type == FolderPainterPathCheckType.EndsWith && path.EndsWith(rule.PathRule.Value))
                {
                    DrawWithVisualConfig(rule.Visual, rect, guid, path);
                    return;
                }
                else if (rule.PathRule.Type == FolderPainterPathCheckType.StartsWith && path.StartsWith(rule.PathRule.Value))
                {
                    DrawWithVisualConfig(rule.Visual, rect, guid, path);
                    return;
                }
            }
        }

        private void DrawWithVisualConfig(FolderPainterVisualEVO visualRuleSet, Rect rect, string guid, string path)
        {
            if (visualRuleSet == null) return;

            Rect area = new Rect(0, rect.y, rect.width + rect.x, rect.height);

            DrawGradient(area, visualRuleSet.ColorInfo.StartColor, visualRuleSet.ColorInfo.EndColor);

            if (visualRuleSet.Text.OverrideFont)
            {
                string label = "";
                if (visualRuleSet.Text.OverrideLabel)
                {
                    label = visualRuleSet.Text.Label;
                }
                else
                {
                    var parts = path.Split("/");
                    label = parts[^1];
                }

                DrawLabel(rect, visualRuleSet.Text.TextOffset, label, visualRuleSet.Text.Color, visualRuleSet.Text.Style);
            }

            if (visualRuleSet.Selection.OverrideSelectionColor)
            {
                if (Selection.assetGUIDs.Contains(guid))
                    DrawSelection(rect, true, visualRuleSet.Selection.Color);
            }

            if (visualRuleSet.Icon.Enable)
            {
                DrawIcon(rect, visualRuleSet.Icon.Texture, visualRuleSet.Icon.Size, visualRuleSet.Icon.OffsetScaleX, visualRuleSet.Icon.PixelOffsetX, visualRuleSet.Icon.PixelOffsetY);
            }
        }

        private void DrawGradient(Rect rect, Color startColor, Color endColor)
        {
            Matrix4x4 matrix = GUI.matrix;
            Rect gradientRect = rect;

            for (int i = 0; i < rect.width; i++)
            {
                var t = i / rect.width;
                var color = Color.Lerp(startColor, endColor, t);
                gradientRect.x = rect.x + i;
                gradientRect.width = 1;

                EditorGUI.DrawRect(gradientRect, color);
            }

            GUI.matrix = matrix;
        }

        private void DrawLabel(Rect rect, float offset, string label, Color fontColor, FontStyle fontStyle)
        {
            GUIStyle style = new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = fontColor },
                fontStyle = fontStyle
            };

            rect.x += offset;

            EditorGUI.LabelField(rect, label, style);
        }

        private void DrawSelection(Rect inSelectionRect, bool isSelected, Color selectionColor)
        {
            if (isSelected)
            {
                Rect backgroundRect = inSelectionRect;
                backgroundRect.x = 0;
                backgroundRect.xMax *= 1.5f;

                EditorGUI.DrawRect(backgroundRect, selectionColor);
            }
        }

        private void DrawIcon(Rect rect, Texture tex, Vector2 iconSize, float offsetX, float pixelOffsetX, float pixelOffsetY)
        {
            if (tex == null) return;

            float weight = iconSize.x <= 0 ? 16 : iconSize.x;
            float height = iconSize.y <= 0 ? 16 : iconSize.y;

            Rect r = new Rect(rect.x * offsetX + pixelOffsetX, rect.y + pixelOffsetY, weight, height);

            GUI.DrawTexture(r, tex);
        }
    }
}
#endif
