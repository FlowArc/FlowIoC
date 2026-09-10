#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The Function generator. What the window is actually for is the base type: the parameters,
    /// the return type and the base's generic arguments all have to agree, and that is the part a
    /// hand-written function gets wrong.
    /// </summary>
    internal class CreateFunctionPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreateFunctionPage() : base(null)
        {
        }

        public override string Title => "Create Function";

        public override FlowIcon Icon => FlowIcon.ArrowReturn;

        protected override string BodyHeadline => "Tools > FlowIoC > Create Function.";

        protected override string BodyTagline =>
            "One file in the same Controllers folder the Commands are in, deriving from the shipped "
            + "arity that matches what you asked for.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateFunctionWindow.png"),
                "Create Function. The preview under the form is the call, not the class.");

            painter.SubHeading("There is nothing to bind");
            painter.Paragraph(
                "A Function is called from inside a Command's Execute rather than dispatched, so the "
                + "generator writes the one file and touches no Context. Which of the two a piece of "
                + "work is, and why a Function is invisible in the Flow Console, is on the "
                + "Controllers page.");
            painter.PageLink("Controllers", "Read: Controllers");

            painter.Separator();
            painter.SubHeading("The kind decides the base");
            painter.Paragraph(
                "A Function derives from one of the shipped arities and never from FunctionBody - "
                + "its constructor is internal, so the compiler says so. The window picks the base "
                + "and writes Execute to match it.");

            painter.Rule("Void - work with no answer.");
            painter.Code(
                "public class MarkHudDirtyFunction : FunctionVoid\n"
                + "{\n"
                + "    public override void Execute() => _hudModel.MarkDirty();\n"
                + "}");

            painter.Space();
            painter.Rule("Return - one value back.");
            painter.Code(
                "public class CalculateDamageFunction : FunctionReturn<double, string>\n"
                + "{\n"
                + "    public override double Execute(string weaponId) =>\n"
                + "        _weapons.Get(weaponId).Damage;\n"
                + "}");

            painter.Space();
            painter.Rule("Async - a coroutine that answers through a callback.");
            painter.Paragraph(
                "An async function's type argument is the value its callback carries rather than a "
                + "parameter, which is why the parameter rows are not offered for one. It answers "
                + "through FunctionCompletedCallback.");

            painter.Separator();
            painter.SubHeading("Parameters");
            painter.Paragraph(
                "Up to four, and that is where the shipped arities stop. A function that wants a "
                + "fifth takes a value object instead, which is what the fifth parameter was going "
                + "to be anyway.");

            painter.Note(
                "Important: the preview is the call. A Function says nothing about where it is "
                + "called from - that is the whole difference between one and a Command - so the "
                + "call is the part nobody can read off the file, and it changes with every field "
                + "above it.");

            painter.Separator();
            painter.SubHeading("Parent Module");
            painter.Paragraph(
                "The same list Create Command offers, and the same folder: a module keeps its "
                + "Commands and its Functions together, both being controllers.");
            painter.PageLink("Create Command", "Writing a Command instead");
        }
    }
}

#endif
