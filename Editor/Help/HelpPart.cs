#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// One card in the row a page opens with, so that what a thing is made of is a picture before
    /// it is a paragraph: Controllers is a Command card beside a Function card, and a reader knows
    /// the shape of the topic before reading a word of it.
    ///
    /// The class is public for the same reason HelpPainter is - a private module in another
    /// package writes its page against these marks.
    /// </summary>
    public class HelpPart
    {
        public HelpPart(string title, string summary, string signature = null,
            FlowIcon icon = FlowIcon.None)
        {
            Title = title;
            Summary = summary;
            Signature = signature;
            Icon = icon;
        }

        public string Title { get; }

        /// <summary>The one line that says what the part is for. Two at the very most.</summary>
        public string Summary { get; }

        /// <summary>
        /// What the part looks like in code, drawn under a hairline at the foot of the card. Empty
        /// for a part that has no one line shape.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// The drawing beside the title, one of FlowIoC's own the way a page's is. None for a card
        /// that carries no icon.
        /// </summary>
        public FlowIcon Icon { get; }
    }
}

#endif
