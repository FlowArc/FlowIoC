#if UNITY_EDITOR

namespace FlowIoC.Editor.Icons
{
    /// <summary>
    /// The pictures FlowIoC's own windows draw beside a name. Every value is one drawing under
    /// Editor/Icons, shipped at the pixel sizes the windows draw it at - 16 and 24 points, each
    /// at 1x and 2x - so an icon is always drawn at the size it was rasterised for and never
    /// resampled. The sources are the SVG files under Editor/Icons/Source~, every one drawn on the
    /// same 16 by 16 grid in the same stroke, which is what makes a column of them read as one set.
    ///
    /// A value is named for what it depicts rather than for where it is used, so the same drawing
    /// can stand for the same idea on more than one page: Controllers and Create Command both wear
    /// the bolt.
    ///
    /// Every value carries its number. Unity serialises an enum as an int, so a value inserted in
    /// the middle would renumber everything below it. A new icon takes the next free number, and
    /// a number a deleted icon used is never given to another.
    /// </summary>
    public enum FlowIcon
    {
        None = 0,
        ArrowReturn = 1,
        Bolt = 2,
        Book = 3,
        Braces = 4,
        Broadcast = 5,
        Brush = 6,
        Camera = 7,
        Cube = 8,
        Database = 9,
        Diagram = 10,
        Eye = 11,
        Folder = 12,
        Gear = 13,
        Grid = 14,
        Info = 15,
        Layers = 16,
        Link = 17,
        ListNested = 18,
        Lock = 19,
        Pencil = 20,
        Plus = 21,
        Puzzle = 22,
        Robot = 23,
        Search = 24,
        SortDown = 25,
        Stopwatch = 26,
        Terminal = 27,
        Trash = 28,
        Wand = 29,
        Window = 30,
        Windows = 31,
        Wrench = 32
    }
}

#endif
