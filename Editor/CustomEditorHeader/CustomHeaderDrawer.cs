using System;
using FlowIoC.BaseModule.Attributes;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CustomEditorHeader
{
    public static class CustomHeaderDrawer
    {
        private static HeaderConfig _defaultConfig;
        
        private static HeaderConfig DefaultConfig
        {
            get
            {
                if (_defaultConfig == null)
                {
                    _defaultConfig = ScriptableObject.CreateInstance<HeaderConfig>();
                }
                return _defaultConfig;
            }
        }

        public static void DrawHeader(string title, HeaderConfig config = null)
        {
            var headerConfig = config ?? DefaultConfig;
            DrawHeaderInternal(title, headerConfig.HeaderBackgroundColor, headerConfig.HeaderBackgroundColor, 
                headerConfig.HeaderHeight, headerConfig.HeaderFontSize, headerConfig.HeaderPrefix, headerConfig.HeaderSuffix);
        }

        public static void DrawHeaderFromAttribute(UnityEngine.Object target)
        {
            var type = target.GetType();
            var attribute = Attribute.GetCustomAttribute(type, typeof(CustomClassHeaderAttribute)) as CustomClassHeaderAttribute;
            
            if (attribute != null)
            {
                DrawHeaderInternal(attribute.Title, attribute.StartColor, attribute.EndColor, 
                    22, attribute.FontSize, attribute.Prefix, attribute.Suffix);
            }
        }

        private static void DrawHeaderInternal(string title, Color startColor, Color endColor, 
            int height, int fontSize, string prefix, string suffix)
        {
            var headerRect = EditorGUILayout.GetControlRect(false, height);
            //Debug.Log("---" + headerRect);
            var fullRect = new Rect(0, headerRect.y, headerRect.width + headerRect.x, headerRect.height);
            if (startColor == endColor)
            {
                EditorGUI.DrawRect(fullRect, startColor);
            }
            else
            {
                DrawGradient(fullRect, startColor, endColor);
            }
            
            var headerContent = new GUIContent($"{prefix}{title}{suffix}");
            
            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.white },
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            
            EditorGUI.LabelField(headerRect, headerContent, style);
        }

        private static void DrawGradient(Rect rect, Color startColor, Color endColor)
        {
            var matrix = GUI.matrix;
            var gradientRect = rect;
            
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
        
    }
} 