using UnityEngine;

namespace FlowIoC.Editor.CustomEditorHeader
{
    public class ED_Header : ScriptableObject
    {
        public Color HeaderBackgroundColor = new Color(0.8f, 0.4f, 0.0f);
        public Color HeaderTextColor = Color.white;
        public int HeaderHeight = 22;
        public int HeaderFontSize = 12;
        public FontStyle HeaderFontStyle = FontStyle.Bold;
        public string HeaderPrefix = "";
        public string HeaderSuffix = "";
    }
} 