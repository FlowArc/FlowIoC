namespace Modules.AbTestFlowModule.Enums
{
    /// <summary>
    /// What part of a test a validation message is about, so the editor says it beside that part:
    /// the test as a whole, one group's column, one cell of the matrix, or the rows as a whole -
    /// a test that has none.
    /// </summary>
    public enum AbTestValidationScope
    {
        Test = 0,
        Group = 1,
        Cell = 2,
        Rows = 3
    }
}
