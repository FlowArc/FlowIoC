using System;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    [Serializable]
    public class FlowLogProfileData
    {
        public string Name;

        [Tooltip("If true, this profile cannot be removed")]
        public bool IsMandatory;

        [Tooltip("If true, this profile can be edited in the inspector")]
        public bool IsEditable = true;

        public string Prefix;
        public FlowTextStyle PrefixStyle;
        public Color PrefixColor = Color.white;

        public FlowTextStyle MessageStyle;
        public Color MessageColor = Color.white;

        public string Postfix;
        public FlowTextStyle PostfixStyle;
        public Color PostfixColor = Color.white;

        public FlowLogProfile ToProfile()
        {
            var profile = new FlowLogProfile();

            if (!string.IsNullOrEmpty(Prefix))
                profile.SetPrefix(Prefix, PrefixStyle, PrefixColor);

            if (MessageStyle != FlowTextStyle.None)
                profile.SetMessageStyle(MessageStyle);

            if (MessageColor != Color.white)
                profile.SetMessageColor(MessageColor);

            if (!string.IsNullOrEmpty(Postfix))
                profile.SetPostfix(Postfix, PostfixStyle, PostfixColor);

            return profile;
        }

        public bool IsEffective()
        {
            return !string.IsNullOrEmpty(Prefix) ||
                   !string.IsNullOrEmpty(Postfix) ||
                   MessageStyle != FlowTextStyle.None ||
                   MessageColor != Color.white;
        }
    }
}
