using UnityEngine;

namespace Modules.DeviceDebuggerModule.Data.ValueObjects
{
    /// <summary>A row's two lines, as LogRowTextRule split them.</summary>
    public class LogRowTextVO
    {
        /// <summary>The first line of the message, rich text kept, the leading channel tag taken off.</summary>
        public string Message = "";

        /// <summary>The whole message with every rich-text tag stripped, for the detail and the clipboard.</summary>
        public string PlainMessage = "";

        /// <summary>The channel tag, "[Signal]"; from the message when it carried one, else the channel name.</summary>
        public string Tag = "";

        public Color TagColor = Color.white;

        /// <summary>File and line, the class, or the kind - never empty.</summary>
        public string Source = "";
    }
}
