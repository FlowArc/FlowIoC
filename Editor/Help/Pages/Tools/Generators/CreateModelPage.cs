#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The Model generator. It writes the pair and binds it, and the toggle beside the name is the
    /// part worth explaining: a dummy is an Editor affordance a test context swaps in, and a build
    /// never sees it.
    /// </summary>
    internal class CreateModelPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreateModelPage() : base(null)
        {
        }

        public override string Title => "Create Model";

        public override FlowIcon Icon => FlowIcon.Database;

        protected override string BodyHeadline => "Tools > FlowIoC > Create Model.";

        protected override string BodyTagline =>
            "The IPlayerModel interface and the PlayerModel behind it, in the module's Models "
            + "folder, bound in the module's Context.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateModelWindow.png"),
                "Create Model, writing the pair into the module picked below it.");

            painter.SubHeading("Two files, one name");
            painter.Paragraph(
                "Type Player and the window writes IPlayerModel and PlayerModel. The interface is "
                + "what everything else injects, which is what lets a test module bind something "
                + "else in its place without the Commands knowing.");

            painter.Separator();
            painter.SubHeading("The binding it writes");
            painter.Paragraph(
                "The line goes into the Context's InjectionBindings, through the module's own "
                + "binder. That is the right default: a model is the module's state, and a "
                + "neighbour that reads it is a coupling somebody has to maintain.");
            painter.Code(
                "public override void InjectionBindings()\n"
                + "{\n"
                + "    base.InjectionBindings();\n"
                + "    InjectionBinder.Bind<IPlayerModel,PlayerModel>();\n"
                + "}",
                "PlayerContext.cs - written for you");
            painter.Paragraph(
                "A model other modules genuinely read is the exception, and it is your edit: change "
                + "InjectionBinder to InjectionBinderCrossContext and the model is reachable from "
                + "any context in the scene.");

            painter.Note(
                "Important: a Model never subscribes to a signal. Nothing reaches in and changes "
                + "its state - an incoming signal runs a Command, and the Command calls the Model. "
                + "Announcing that one of its own values changed is allowed; listening is not.");
            painter.PageLink("Model", "Read: Model");

            painter.Separator();
            painter.SubHeading("Create Dummy Model");
            painter.Paragraph(
                "Ticked, the window writes a PlayerDummyModel beside the pair and binds all three. "
                + "The three-argument overload hands a test context the dummy and everything else "
                + "the real one, so a test module runs against a stand-in without a second binding "
                + "of its own.");
            painter.Code(
                "InjectionBinder.Bind<IPlayerModel, PlayerModel,PlayerDummyModel >();");
            painter.Note(
                "The dummy is an Editor affordance only. The swap sits behind UNITY_EDITOR and is "
                + "made on Context.IsTest, so a build always gets the real implementation however a "
                + "Root was left ticked in a scene.");

            painter.Separator();
            painter.SubHeading("Injectables and Parent Module");
            painter.Paragraph(
                "Each injectable row is written onto the model as a property, because injection "
                + "targets properties and a plain field is skipped silently. Any module may hold a "
                + "model, so every module is offered, and the pair lands in that module's Models "
                + "folder. Data two modules need is not a model two modules bind - it is data one "
                + "module publishes through its Shared assembly, or a Service.");
            painter.PageLink("Systems and Services", "Read: Systems and Services");
        }
    }
}

#endif