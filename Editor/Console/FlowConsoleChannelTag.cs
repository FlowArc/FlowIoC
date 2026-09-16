#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The tag a channel writes at the front of its lines - "[Signal]" for a framework channel,
    /// "[Player]" for a module - picked off the drawn string, so the row can move it onto the
    /// source line and paint a plate behind it. The tag was baked into the message as rich text
    /// when the line was logged, and IMGUI's rich text has no background tag; so the row takes the
    /// tag off the message, draws it where it wants it, and this is what hands the row the tag.
    ///
    /// A line Unity wrote carries an icon for a tag and nothing in the text, and a channel whose
    /// profile decorates nothing - Default, a module whose card says "Profile: none" - carries none.
    /// </summary>
    internal class FlowConsoleChannelTag
    {
        private readonly Dictionary<string, string> _tagByChannel =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The tag at the front of the message - as the logger wrote it, colour tags included, so
        /// the row measures exactly what it draws - and the colour it is written in. False when
        /// the row carries no tag: a line Unity wrote, a channel with no tag, or a line that does
        /// not begin with its channel's tag.
        /// </summary>
        internal bool TryFind(FlowLogChannel channel, string message, out string tag, out Color color)
        {
            tag = null;
            color = default;

            if (channel == null || string.IsNullOrEmpty(message)) return false;

            string written = TagOf(channel);
            if (written == null || !message.StartsWith(written, StringComparison.Ordinal)) return false;

            tag = written;
            color = channel.Profile.PrefixColor;
            return true;
        }

        /// <summary>
        /// Kept once it is built: a channel's profile is code - the framework's table, or a
        /// module's generated part - so what its tag looks like does not change while the domain
        /// is loaded, and the rows ask on every repaint.
        /// </summary>
        private string TagOf(FlowLogChannel channel)
        {
            if (_tagByChannel.TryGetValue(channel.Name, out string tag)) return tag;

            tag = channel.Profile != null ? FlowLogger.FormatPrefix(channel.Profile) : null;
            _tagByChannel[channel.Name] = tag;
            return tag;
        }
    }
}
#endif
