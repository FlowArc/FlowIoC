#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The Command generator. Its interesting half is the Bind toggle, which writes into the
    /// module's Context as well as into Controllers - the only single-file generator that touches
    /// a second file.
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
            "One file in the module's Controllers folder, with the right namespace - and, if you "
            + "ask for it, the binding in the Context that decides when it runs.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateCommandWindow.png"),
                "Create Command, with Bind ticked and the signal it answers named.");

            painter.SubHeading("The name");
            painter.Paragraph(
                "Type the verb and its object; the window adds Command and shows you the result "
                + "under the field. Name a Command after what it does to the world - GrantItemCommand, "
                + "PersistMatchResultCommand - and never after the signal that triggers it. A command "
                + "named OnPurchaseCommand cannot be bound into a second sequence without lying "
                + "about itself.");

            painter.Separator();
            painter.SubHeading("Bind");
            painter.Paragraph(
                "Tick it and two more fields appear: the signal holder's class name, and the signal "
                + "on it. The generator then writes into the module's Context the InjectSignal "
                + "property for that holder, if it is not already there, and the binding that runs "
                + "this command.");
            painter.Code(
                "// Bind ticked, PlayerSignals / DecreaseCurrency, Is Sequel on:\n"
                + "CommandBinder.Bind(_playerSignals.Incoming.DecreaseCurrency)\n"
                + "    .ToSequence<DecreaseCurrencyCommand>();",
                "PlayerContext.cs - written for you");
            painter.Paragraph(
                "Is Sequel is the choice between the two terminators: on writes ToSequence, off "
                + "writes ToParallel. A step that needs what the step before it did is a sequence "
                + "step; one that touches nobody else's result is a parallel one.");

            painter.Note(
                "Important: the holder and the signal are typed as text, and nothing checks that a "
                + "signal of that name exists. A typo is written into the Context and reported by "
                + "the C# compiler against the Context rather than by this window. Leave Bind clear "
                + "and write the binding yourself when the signal is not there yet.");

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
                "Any module may host a command, so every module in the project is offered. The file "
                + "lands in that module's Controllers folder, which is where its Functions are too - "
                + "both are controllers.");
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
