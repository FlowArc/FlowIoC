using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    public class FlowLogProfile
    {
        public string Prefix { get; private set; }
        public string Postfix { get; private set; }
        public FlowTextStyle PrefixStyle { get; private set; }
        public FlowTextStyle PostfixStyle { get; private set; }
        public FlowTextStyle MessageStyle { get; private set; }
        public Color PrefixColor { get; private set; } = Color.white;
        public Color MessageColor { get; private set; } = Color.white;
        public Color PostfixColor { get; private set; } = Color.white;

        public FlowLogProfile SetPrefix(string prefix, FlowTextStyle style = FlowTextStyle.None, Color? color = null)
        {
            Prefix = prefix;
            PrefixStyle = style;
            PrefixColor = color ?? Color.white;
            return this;
        }

        public FlowLogProfile SetPrefix(string prefix, string hex)
        {
            return SetPrefix(prefix, FlowTextStyle.None, ParseHex(hex));
        }

        public FlowLogProfile SetPrefix(string prefix, FlowTextStyle style, string hex)
        {
            return SetPrefix(prefix, style, ParseHex(hex));
        }

        public FlowLogProfile SetPostfix(string postfix, FlowTextStyle style = FlowTextStyle.None, Color? color = null)
        {
            Postfix = postfix;
            PostfixStyle = style;
            PostfixColor = color ?? Color.white;
            return this;
        }

        public FlowLogProfile SetPostfix(string postfix, string hex)
        {
            return SetPostfix(postfix, FlowTextStyle.None, ParseHex(hex));
        }

        public FlowLogProfile SetPostfix(string postfix, FlowTextStyle style, string hex)
        {
            return SetPostfix(postfix, style, ParseHex(hex));
        }

        public FlowLogProfile SetMessageStyle(FlowTextStyle style)
        {
            MessageStyle = style;
            return this;
        }

        public FlowLogProfile SetMessageColor(Color color)
        {
            MessageColor = color;
            return this;
        }

        public FlowLogProfile SetMessageColor(string hex)
        {
            return SetMessageColor(ParseHex(hex));
        }

        public FlowLogProfile SetColor(Color color)
        {
            PrefixColor = color;
            MessageColor = color;
            PostfixColor = color;
            return this;
        }

        public FlowLogProfile SetColor(string hex)
        {
            return SetColor(ParseHex(hex));
        }

        private static Color ParseHex(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
        }
    }
}
