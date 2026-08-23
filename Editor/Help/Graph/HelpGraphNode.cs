#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Graph
{
    /// <summary>
    /// One box in a help diagram. Row and Column are cells, not pixels: the painter decides how
    /// large a cell is, so a page never carries a layout number.
    /// </summary>
    internal class HelpGraphNode
    {
        public HelpGraphNode(string id, string title, string subtitle, int row, int column)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Row = row;
            Column = column;
        }

        public string Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public int Row { get; }
        public int Column { get; }
    }
}

#endif
