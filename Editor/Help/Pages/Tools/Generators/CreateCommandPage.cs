#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The Command generator. It writes the one file, and shows the binding the Context needs
    /// beside it rather than writing that too - where a command sits in a sequence is a decision
    /// about the flow, and the window does not take it.
    /// </summary>
    internal class CreateCommandPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreateCommandPage() : base(null)
        {
        }

        public override string Title => "Create Command";

        public override string Icon => "cs Script Icon";

        protected override string BodyHeadline => "Tools > FlowIoC > Create Command.";

        protected override string BodyTagline =>
            "One file in the module's Controllers folder, with the right namespace - and the two "
            + "lines the Context needs to run it, ready to paste where the flow reads right.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateCommandWindow.png"),
                "Create Command, with the binding it shows beside the file it writes.");

            painter.SubHeading("The name");
            painter.Paragraph(
                "Type the verb and its object; the window adds Command and shows you the result "
                + "beside the field. Name a Command after what it does to the world - GrantItemCommand, "
                + "PersistMatchResultCommand - and never after the signal that triggers it. A command "
                + "named OnPurchaseCommand cannot be bound into a second sequence without lying "
                + "about itself.");

            painter.Separator();
            painter.SubHeading("How it is bound");
            painter.Paragraph(
                "A command runs when a signal it is bound to is dispatched, and that binding lives "
                + "in the module's Context. The window shows it rather than writing it: two lines, "
                + "spelled against the holder field the picked module's Context declares, with the "
                + "Incoming signal named after the command, and a Copy button beside them.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.DecreaseCurrency)\n"
                + "    .ToSequence<DecreaseCurrencyCommand>();",
                "Paste it into CommandBindings, where the flow reads right");
            painter.Paragraph(
                "Where the command sits is yours to decide - which sequence it joins, which step it "
                + "follows, whether it is a ToSequence step that needs what the step before it did "
                + "or a ToParallel one that touches nobody else's result. That is a decision about "
                + "the flow, and a flow is read from one Context, so it is made there and not in a "
                + "window that cannot see the sequence it would be joining.");
            painter.PageLink("Controllers", "Read: Controllers");

            painter.Separator();
            painter.SubHeading("Injectables");
            painter.Paragraph(
                "Each row is a type the command injects, written as a property, because injection "
                + "targets properties and a plain field is skipped silently. List the module's Model "
                + "and whatever Services the work needs. A Command that has to reach another "
                + "module's Model is a Command in the wrong module.");

            painter.Separator();
            painter.SubHeading("Parent Module");
            painter.Paragraph(
                "Any module may host a command, so every module in the project is offered, as the "
                + "tree Module Scanner draws - a module under the module it lives in - and clicking "
                + "a row picks it. The file lands in that module's Controllers folder, which is "
                + "where its Functions are too - both are controllers. Picked into a test module, "
                + "the file comes out wrapped in UNITY_EDITOR, the way every script under "
                + "zTestModules has to be; the same holds for a Function, a Model and a View.");
            painter.PageLink("Create Function", "Writing a Function instead");

            painter.Note(
                "Important: the destination comes from the project's module index, and the index is "
                + "read off the folder tree. Move a module's folder without running Module Scanner "
                + "and the next command is written into the module's old namespace.");
            painter.PageLink("Module Scanner", "Read: Module Scanner");
        }
    }
}

#endif