#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The local save module: what it persists, how a game asks for a write, what the file on
    /// disk is, the password that encrypts it, and the button that puts it in the project.
    /// </summary>
    internal class LocalSaveModulePage : ModulePage
    {
        public override string ModuleFolderName => "LocalSaveModule";

        public override string Title => "Local Save";

        public override string Subtitle => "ScriptableObjects that survive a restart";

        public override FlowIcon Icon => FlowIcon.Database;

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "Put LocalSaveRoot in the scene and file the assets to keep on its adapter.",
                "The name an asset is filed under is the key it is stored against. A key never "
                + "written is skipped, so the values authored in the Editor are the defaults."),
            new HelpTab("Usage", DrawUsage,
                "Bind ILocalSaveService.Commands.Save as the step after the one that changed the data.",
                "The asset's name is given at the binding, so the flow reads from the Context: what "
                + "changed, and that it was written. SaveAll writes everything; the module sends it "
                + "to itself on pause and on quit."),
            new HelpTab("Storage", DrawStorage,
                "One JSON file of the module's own, plain or encrypted with a password.",
                "SaveFile.flowsave under the persistent data path, one member per asset. The "
                + "password on the Root's adapter turns encryption on; empty, the file is one a "
                + "developer reads.")
        };

        public override string InstalledHint =>
            "Drop LocalSaveRoot into your scene and file the assets to keep on its adapter; the "
            + "Setup tab has the steps.";

        public override string BodyHeadline =>
            "Drop an asset on the Root, and it is written when you say and back before anything reads it.";

        public override string BodyTagline =>
            "A profile, a settings asset, the map progress - each is a ScriptableObject filed on one "
            + "adapter, and restoring it needs no code in the module that owns it.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "Everything filed on LocalSaveRoot's adapter is written to disk when you ask, and "
                + "read back before any other module wakes up - the restore runs in the module's "
                + "PostConstruct, ahead of the band the game's own Roots use.");
            painter.Bullet(
                "A file of its own. No vendor asset, no manager component in the scene, nothing to "
                + "enable before the module compiles: SaveFile.flowsave under "
                + "Application.persistentDataPath, and the module writes it to a temporary file "
                + "first so a player killed mid-write keeps the previous save whole.");
            painter.Bullet(
                "Dictionary members and [SerializeReference] fields survive the round trip. "
                + "Newtonsoft does the serializing, narrowed back to Unity's own rules - public "
                + "fields and [SerializeField] ones, no properties, no references to other assets.");
            painter.Bullet(
                "Encryption is one field on the adapter. Leave the password empty and the file is "
                + "one a developer reads; set it and the file is AES with the key derived from it.");
            painter.Bullet(
                "In the Editor the assets are put back to what they were when play mode ends, so "
                + "a play session leaves no diff on an asset nobody edited.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.LocalSave and binds "
                + "ILocalSaveService.Commands.Save as a step. It has no signals: the module that "
                + "changed the data is the one that knows it is time to write.");

            painter.SubHeading("What it is not");
            painter.Paragraph(
                "Not a cloud save, not save slots, and not a secret. The password keeps a player "
                + "from opening the file in a text editor and changing a number; it does not keep "
                + "a determined one out, because the password is a string in the build.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("Persisting an asset");
            painter.Paragraph(
                "Drop the ScriptableObject onto the adapter on LocalSaveRoot. That is the whole "
                + "step - the name you file it under is the key it is stored against, and "
                + "restoring it needs no code in your module at all.");
            painter.Paragraph(
                "A key that was never written is skipped, so the asset keeps the values you "
                + "authored in the Editor. Those are your defaults; there is nowhere else to write "
                + "them.");
            painter.Space();

            painter.SubHeading("Ordering");
            painter.Paragraph(
                "LocalSaveRoot sits at Initialize Order -100, ahead of the band the game's own "
                + "modules use. PostConstruct runs during the binding pass rather than after Setup, "
                + "and each Root finishes its own before the next begins - so sitting ahead of band "
                + "0 is what puts the saved data in place before another module's model reads it. "
                + "Do not raise the number above 0.");
            painter.Space();

            painter.SubHeading("In the Editor");
            painter.Paragraph(
                "Loading writes into the ScriptableObject asset, and in the Editor that change "
                + "would outlive play mode and land in source control as a diff on an asset nobody "
                + "edited. The module snapshots every registered asset before loading and restores "
                + "it on quit, so the save system runs normally in the Editor and still leaves no "
                + "diff behind.");
            painter.Paragraph(
                "Tick IsTest on LocalSaveRoot to switch the whole thing off for a scene: nothing "
                + "is read and nothing is written. A build always gets the real service.");
            painter.Space();

            painter.SubHeading("Trying it");
            painter.Paragraph(
                "The module ships the scene it runs in. Open LocalSaveTestScene under "
                + "zTestModules, press Play, click Increment a few times, leave play mode and come "
                + "back - the counter is where you left it, and the probe asset is untouched in "
                + "source control.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("Saving one asset");
            painter.Paragraph(
                "Bind the step after the one that changed the data, with the asset's name as it "
                + "was filed on the adapter. The flow then reads from the Context: what changed, "
                + "and that it was written.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.AddCurrency)\n"
                + "    .ToSequence<AddCurrencyCommand>()\n"
                + "    .ToSequence<ILocalSaveService.Commands.Save>(nameof(PD_Profile));");
            painter.Space();

            painter.SubHeading("Saving everything");
            painter.Paragraph(
                "SaveAll writes every filed asset at once. The module sends it to itself when the "
                + "application pauses or quits, because a backgrounded mobile app is often killed "
                + "without ever quitting cleanly; a flow that ends a session binds it the same way.");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.LeaveMatch)\n"
                + "    .ToSequence<CloseMatchCommand>()\n"
                + "    .ToSequence<ILocalSaveService.Commands.SaveAll>();");
            painter.Space();

            painter.SubHeading("Reading");
            painter.Paragraph(
                "There is nothing to call. The asset you filed holds the saved values by the time "
                + "your own module's PostConstruct runs, so a Model reads it off its adapter the way "
                + "it reads any other asset.");
        }

        private void DrawStorage(HelpPainter painter)
        {
            painter.SubHeading("The file");
            painter.Paragraph(
                "SaveFile.flowsave under Application.persistentDataPath: one JSON object, one "
                + "member per asset under the name it was filed as. The extension is FlowIoC's "
                + "own rather than .json so the file does not invite a player to open it in "
                + "whatever their system pairs with that name; what is inside is JSON all the same.");
            painter.Code(
                "{\n"
                + "  \"PD_Profile\": {\n"
                + "    \"Coins\": 120,\n"
                + "    \"Levels\": { \"forest\": 3 }\n"
                + "  }\n"
                + "}");
            painter.Paragraph(
                "Nothing reaches disk until a Save step flushes. The write goes to a temporary "
                + "file first and is moved over the old one only when it is whole, so a player "
                + "killed mid-write is left with the previous save rather than half of a new one.");
            painter.Space();

            painter.SubHeading("The password");
            painter.Paragraph(
                "Set it on LocalSaveRoot's adapter and the file is AES-256, the key derived from "
                + "the password with a fresh salt on every write. Leave it empty and the file is "
                + "plain.");
            painter.Paragraph(
                "Reading goes by the file, not the setting: a plain file is read whatever the "
                + "password says, and the next write is the encrypted one. So a game that turns "
                + "encryption on after shipping keeps every save its players have. An encrypted "
                + "file read with no password, or the wrong one, is logged as an error and the "
                + "session starts empty.");
            painter.Note(
                "What the password protects against is a player opening the file in a text editor "
                + "and changing a number. It is not a secret from a determined one: the password "
                + "is a string in the build.");
            painter.Space();

            painter.SubHeading("What is stored");
            painter.Paragraph(
                "Newtonsoft does the serializing, narrowed back to Unity's own rules: public fields "
                + "and [SerializeField] ones are saved, properties are not, and a field pointing at "
                + "another asset or scene object is left out because a reference means nothing to "
                + "the session that reads the file. Dictionary members and [SerializeReference] "
                + "fields both survive the round trip.");
            painter.Paragraph(
                "A load overwrites rather than merges, so a collection is replaced and an entry the "
                + "player deleted stays deleted.");
            painter.Paragraph(
                "Carrying a field declared as an interface or a base class means the file names the "
                + "type to build, and a save file sits in the player's own folder where it can be "
                + "edited. So a name in the file may only resolve to your project's assemblies: a "
                + "runtime or engine type is refused and the refusal is logged.");
        }
    }
}

#endif
