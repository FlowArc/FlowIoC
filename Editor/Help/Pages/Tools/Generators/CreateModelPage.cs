#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The Model generator. The file it writes is the easy half; which binder the pair is bound
    /// through is the decision, and the window deliberately leaves it to the reader.
    /// </summary>
    internal class CreateModelPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreateModelPage() : base(null)
        {
        }

        public override string Title => "Create Model";

        public override string Icon => "cs Script Icon";

        protected override string BodyHeadline => "Tools > FlowIoC > Create Model.";

        protected override string BodyTagline =>
            "The IPlayerModel interface and the PlayerModel behind it, in the module's Models "
            + "folder with the right namespace.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateModelWindow.png"),
                "Create Model, writing the pair into the module picked below it.");

            painter.SubHeading("Two files, one name");
            painter.Paragraph(
                "Type Player and the window writes IPlayerModel and PlayerModel. The interface is "
                + "what everything else injects, which is what lets a test module bind a fake in "
                + "its place without the Commands knowing.");

            painter.Separator();
            painter.SubHeading("The binding is yours");
            painter.Paragraph(
                "The generator writes no binding, because which binder the pair goes through is a "
                + "decision about the game rather than about the file.");
            painter.Code(
                "InjectionBinder.Bind<IPlayerModel, PlayerModel>();              // module-private\n"
                + "InjectionBinderCrossContext.Bind<IPlayerModel, PlayerModel>();  // shared",
                "PlayerContext.cs - InjectionBindings");
            painter.Paragraph(
                "Default to the module-private binder. Cross-context is for a model other modules "
                + "genuinely read, and every one of those is a coupling somebody has to maintain.");

            painter.Note(
                "Important: a Model never subscribes to a signal. Nothing reaches in and changes "
                + "its state - an incoming signal runs a Command, and the Command calls the Model. "
                + "Announcing that one of its own values changed is allowed; listening is not.");
            painter.PageLink("Model", "Read: Model");

            painter.Separator();
            painter.SubHeading("Parent Module");
            painter.Paragraph(
                "Any module may hold a model, so every module is offered, and the pair lands in "
                + "that module's Models folder. A model that two modules need is not a model two "
                + "modules bind - it is data one module publishes through its Shared assembly, or a "
                + "Service.");
            painter.PageLink("Systems and Services", "Read: Systems and Services");
        }
    }
}

#endif
