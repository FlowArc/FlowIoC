#if UNITY_EDITOR

namespace FlowIoC.Editor.Help.Pages.Tools.Generators
{
    /// <summary>
    /// The panel that gives a module that already exists the two things Create Module only offers
    /// on the day it is made. It is a list of every module against both, because the answer to
    /// "which of my modules has a Shared assembly" is otherwise something a reader has to remember.
    /// </summary>
    internal class AddSharedOrSignalsPage : HelpPage
    {
        private readonly HelpImages _images = new HelpImages();

        public AddSharedOrSignalsPage() : base(null)
        {
        }

        public override string Title => "Add Shared or Signals";

        public override string Icon => "Settings";

        protected override string BodyHeadline => "Tools > FlowIoC > Add Shared or Signals.";

        protected override string BodyTagline =>
            "Both are ticks in Create Module, so a module is offered them once - and the need "
            + "usually turns up later, when a second module wants its data or a Connector wants "
            + "its signals.";

        protected override void DrawBody(HelpPainter painter)
        {
            painter.Image(_images.Get("AddSharedSignalsWindow.png"),
                "Every module in the project against the two, with a button where one is missing.");

            painter.SubHeading("Shared - the data a module publishes");
            painter.Paragraph(
                "Add Shared gives a module what Create Module would have: the Scripts/Shared "
                + "folders, an assembly of its own, its namespace settings file, and the references "
                + "from the module, from its own Signals assembly, and from every screen, sub and "
                + "test module already under it. A neighbour then reads a config asset the module "
                + "authored without gaining access to its Models, its Commands or its signals.");
            painter.Paragraph(
                "The Signals assembly gets the reference for a reason of its own: a public signal "
                + "is often generic over a type the module publishes, so the holder's assembly has "
                + "to see the Shared assembly that type lives in. Module Scanner reports it as a "
                + "row of its own for a module that gained the two in some other order.");

            painter.Separator();
            painter.SubHeading("Signals - the module's public surface");
            painter.Paragraph(
                "Add Signals writes the public holder into Scripts/Signals - the folder every "
                + "module already has - along with its assembly and the binding that puts it in the "
                + "module's Context. What a module created without signals lacks is the holder "
                + "inside the folder, not the folder.");
            painter.Paragraph(
                "The two assemblies are separate on purpose. A System or a screen legitimately "
                + "references a neighbour's Shared assembly to read a published enum, and if the "
                + "holder lived there that reference would put the neighbour's signals in scope "
                + "with it. Kept apart, the compiler is what keeps a cross-module Dispatch inside a "
                + "Connector.");
            painter.PageLink("Signals", "Read: Signals");

            painter.Separator();
            painter.SubHeading("Reading the list");
            painter.Paragraph(
                "Every module is listed, whether or not it is offered anything - the list is the "
                + "project's modules rather than a shopping basket. A row that is offered neither "
                + "is drawn grey and says why: a test module publishes and announces nothing, and a "
                + "Connector wires other modules rather than owning signals or data of its own.");
            painter.Paragraph(
                "A row with nothing left to add is not a row that owes anything. It stays green, "
                + "because a module with no Shared assembly is finished as it stands; the buttons "
                + "are what says an offer is available.");

            painter.Note(
                "Every step checks before it writes, so running either on a module that already has "
                + "half of it repairs what is missing and reports that the rest was already in "
                + "place. Refresh re-reads the project after a change made outside the window.");

            painter.Note(
                "Important: an assembly is new to the compiler, so the Editor reloads the domain "
                + "after either button. Anything you were part-way through in another window is "
                + "lost to that reload, the same as after any script change.");
        }
    }
}

#endif