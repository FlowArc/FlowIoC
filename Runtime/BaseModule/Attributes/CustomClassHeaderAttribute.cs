using System;
using UnityEngine;

namespace FlowIoC.BaseModule.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomClassHeaderAttribute : Attribute
    {
        public string Title { get; private set; }
        public string Description { get; set; }
        public Color StartColor { get; private set; }
        public Color EndColor { get; private set; }
        public int FontSize { get; set; } = 12;
        public string Prefix { get; private set; }
        public string Suffix { get; private set; }
        public string ColorHex { get; set; }
        public int Height { get; set; } = 30;

        public CustomClassHeaderAttribute(
            string title, 
            float startR = 0.2f, float startG = 0.6f, float startB = 1f,
            float endR = -1f, float endG = -1f, float endB = -1f,
            int fontSize = 12,
            string prefix = "",
            string suffix = "",
            string description = "")
        {
            Title = title;
            Description = description;
            StartColor = new Color(startR, startG, startB);
            EndColor = endR < 0 ? StartColor : new Color(endR, endG, endB);
            FontSize = fontSize;
            Prefix = prefix;
            Suffix = suffix;
        }
        
        public CustomClassHeaderAttribute(string title, string description = "")
        {
            Title = title;
            Description = description;
            StartColor = new Color(0.2f, 0.6f, 1f);
            EndColor = StartColor;
            FontSize = 12;
            Prefix = "";
            Suffix = "";
        }
        
        public Color GetColor()
        {
            if (!string.IsNullOrEmpty(ColorHex))
            {
                Color color;
                if (ColorUtility.TryParseHtmlString(ColorHex, out color))
                {
                    return color;
                }
            }
            
            return StartColor;
        }
    }
} 