#if UNITY_EDITOR

using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The View generator. The pair it writes is ordinary; what the page has to say is what the
    /// generator does not do - the prefab, the mediation binding - because each of those is a
    /// silent failure when it is missed.
    /// </summary>
    internal class CreateViewPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public CreateViewPage() : base(null)
        {
        }

        public override string Title => "Create View";

        public override FlowIcon Icon => FlowIcon.Window;

        protected override string BodyHeadline => "Tools > FlowIoC > Create View.";

        protected override string BodyTagline =>
            "A View and the Mediator that drives it, written into the module's ViewsMediators "
            + "folder with the actions you listed already on both.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("CreateViewWindow.png"),
                "Create View, with two actions listed under the name.");

            painter.SubHeading("Actions");
            painter.Paragraph(
                "Each row you add is written twice: a method on the View that the scene calls, and "
                + "the handler on the Mediator that answers it. Name them for what the player did - "
                + "NextMapClicked rather than OnRightButtonDown - because the View's job is to "
                + "translate raw input into the action it already offers, and a swipe left should "
                + "reach the same method the button does.");

            painter.Separator();
            painter.SubHeading("What the pair arrives with");
            painter.Paragraph(
                "The View carries RequireComponent(typeof(ViewInjector)), so dropping it onto a "
                + "GameObject brings the injector with it. The Mediator arrives with empty "
                + "OnRegister and OnRemove bodies: whatever you subscribe in the first, unsubscribe "
                + "in the second.");

            painter.Note(
                "Important: a Mediator is pooled, so the mirror is not tidiness. A subscription "
                + "left behind comes back attached to the next view the Mediator is given, and the "
                + "screen answers a signal twice.");

            painter.Separator();
            painter.SubHeading("What it does not do");
            painter.Bullet(
                "It writes no prefab. Create Module builds one for a screen module; a view added to "
                + "an existing module is put in the scene or on a prefab by you.");
            painter.Bullet(
                "It writes no mediation binding. Add it in the module's Context, or the View "
                + "registers against nothing and the Mediator never runs.");
            painter.Code(
                "public override void MediationBindings()\n"
                + "{\n"
                + "    base.MediationBindings();\n"
                + "    MediationBinder.Bind<InventoryView>().To<InventoryMediator>();\n"
                + "}",
                "InventoryContext.cs - written by you");

            painter.Note(
                "Important: a View sits on a child of its Root, never on the Root's own GameObject. "
                + "A view finds its Context by bubbling up the hierarchy and the walk starts at the "
                + "parent, so a view authored on the Root itself is invisible to it - and nothing is "
                + "logged. The view registers against nothing and its Mediator never runs.");
            painter.PageLink("View & Mediator", "Read: View & Mediator");

            painter.Separator();
            painter.SubHeading("A view in a test module");
            painter.Paragraph(
                "Pick the test module and the pair is written into it wrapped in UNITY_EDITOR "
                + "directives, under the namespace its folder gives it. Everything under "
                + "zTestModules is editor-only code, and that is what buys a test module the right "
                + "to reference any module in the project - so every generator wraps what it "
                + "writes into one, and nothing asks you to say so twice.");
        }
    }
}

#endif
