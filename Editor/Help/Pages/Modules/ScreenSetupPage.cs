#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>The ScreenManager and the layers every screen of the game opens into.</summary>
    internal class ScreenSetupPage : ModulePage
    {
        private readonly HelpImages _images = new HelpImages();

        public override string Title => "Screen Module";

        public override string Subtitle => "The ScreenManager and its layers";

        public override FlowIcon Icon => FlowIcon.Layers;

        public override string ModuleFolderName => "ScreenModule";

        public override bool InSetupSet => true;

        public override string BodyHeadline => "Where every screen opens.";

        public override string BodyTagline =>
            "ScreenModule holds the ScreenManager and the layers every screen of the game opens into; "
            + "the screen service in the package drives it.";

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("The ScreenManager", DrawManager,
                "One canvas, ten layers, and no list of screens.",
                "The manager owns the surface a screen is drawn on and the slot it is drawn in. Which "
                + "screens exist is not its business: every screen registers itself with the service "
                + "through its own context, so adding a screen touches nothing here."),
            new HelpTab("Writing a screen", DrawWriting,
                "A screen is a module of its own, and its context says where it opens.",
                "Create Module writes the context, the view, the mediator, the prefab and a test scene "
                + "with the manager in it; the ScreenCVO in the context is the one thing to fill in.")
        };

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it owns");
            painter.Bullet(
                "ScreenRoot, and under it the ScreenManager: a Canvas with its scaler and raycaster, a "
                + "safe-area component, and ten ScreenLayer children - Layer_0 to Layer_9 - that are the "
                + "slots a screen names when it opens.");
            painter.Bullet(
                "ScreenContext, which binds the module's ScreenSignals and nothing else. The "
                + "ScreenServiceRoot beside it in MainScene is the package's: it binds IScreenService, "
                + "the thing every module injects to open a screen. This module is the game's half - "
                + "add a layer, reorder them, put a second manager on a world-space canvas.");
            painter.Bullet(
                "The shipped screens sit elsewhere: MainScreenModule under MainModule and "
                + "GameplayScreenModule under GameplayModule, each in the zScreenModules of the module "
                + "whose feature it shows. The manager here is what they open into.");

            painter.Image(_images.Get("ScreenRootHierarchy.png"),
                "MainScene with ScreenRoot folded open: the manager and its ten layers, under the Core roots.");

            painter.SubHeading("In the scene");
            painter.Table(new[] {"Root", "Initialize Order", "Role"},
                new[]
                {
                    "ScreenRoot", "99",
                    "Core - the frame, declared by [FlowHeader(FlowRole.Core)] on the Root. Up before MainRoot at "
                    + "100, so the layers exist before the flow that opens the first screen; after every "
                    + "module the screens belong to."
                });
            painter.PageLink("Ordering Roots", "Read: Ordering Roots - the bands and why 99");

            painter.Note(
                "This module is the game's from the day it lands. It carries no version and is never "
                + "updated by the package.");
        }

        private void DrawManager(HelpPainter painter)
        {
            painter.SubHeading("Manager, layers and tags");
            painter.Bullet(
                "A manager is one UI surface. Its Manager ID - 0 here - is what the managerId on "
                + "every service call names, and the ScreenCVO of every screen says which manager it "
                + "opens on. One is enough until the game has two surfaces that never meet: a world-space "
                + "canvas beside a screen-space HUD.");
            painter.Bullet(
                "A layer is a slot inside the manager, and one screen occupies one layer at a time. "
                + "The shipped main screen opens in Layer_0 and the gameplay screen in Layer_1; a popup "
                + "takes a higher layer so it draws over whatever is under it. Opening into an occupied "
                + "layer fails - Show answers null - unless the caller says ForceOpenAtFullLayer, which "
                + "hides the occupant first. The refusal is the point: two popups fighting over one slot "
                + "become a null the Command sees rather than an overlap noticed in a screenshot later.");
            painter.Bullet(
                "A tag groups screens across layers and managers for work done in bulk: Load.ByTag "
                + "warms a set during the loading bar, Hide.ScreensByTag clears every overlay of an "
                + "area at once. Default and GroupA to GroupH are the tags on offer.");

            painter.Image(_images.Get("ScreenManagerInspector.png"),
                "The ScreenManager component: its id, and the layer list in the order the slots are numbered.");

            painter.SubHeading("What the manager does not hold");
            painter.Paragraph(
                "No list of screens. A screen registers itself with the screen service when its Root "
                + "binds - the ScreenSubContext it derives from does that - and the service keeps the "
                + "registry. So a screen module installed from the library, or created with Create "
                + "Module, is known to the manager the moment its Root is in the scene, and nothing "
                + "on this component changes.");

            painter.SubHeading("Loaded, shown, hidden, unloaded");
            painter.Paragraph(
                "A screen's prefab is loaded once and pooled. Open shows the pooled instance; Hide takes "
                + "it off the layer and back to the pool, still loaded, so the next Open is cheap and "
                + "the same instance comes back - which is why a screen resets what it changed in "
                + "ScreenHidden rather than relying on a fresh one. Unload destroys the instance and "
                + "releases the asset: hide for \"back\", unload for leaving an area of the game.");
            painter.Paragraph(
                "The boot preloads. PreloadScreensCommand in MainModule asks the service to load the "
                + "screens ahead of the first Open, and reports the step to the loading bar, so the main "
                + "screen appears the frame the boot ends rather than after a load.");
            painter.PageLink("Loading Module", "Read: Loading Module - the boot and its bar");
        }

        private void DrawWriting(HelpPainter painter)
        {
            painter.SubHeading("The context says where it opens");
            painter.Paragraph(
                "A screen module's context derives from ScreenSubContext with its view and mediator, "
                + "and declares a ScreenCVO: the manager, the layer, the tag, how the prefab is loaded, "
                + "and whether the view animates. This is the shipped gameplay screen's:");
            painter.Code(
                "public class GameplayScreenContext : ScreenSubContext<GameplayScreenView, GameplayScreenMediator>\n"
                + "{\n"
                + "    private GameplayScreenSignals _signals;\n"
                + "\n"
                + "    protected override ScreenCVO Screen => new()\n"
                + "    {\n"
                + "        ManagerId        = 0,\n"
                + "        Layer            = 1,\n"
                + "        Tag              = ScreenTag.Default,\n"
                + "        Load             = ScreenLoadCVO.Addressable(\"GameplayScreen\"),\n"
                + "        HasShowAnimation = false,\n"
                + "        HasHideAnimation = false,\n"
                + "    };\n"
                + "\n"
                + "    public override void SignalBindings()\n"
                + "    {\n"
                + "        base.SignalBindings();\n"
                + "        _signals = InjectionBinderCrossContext.Bind<GameplayScreenSignals>();\n"
                + "    }\n"
                + "\n"
                + "    public override void CommandBindings()\n"
                + "    {\n"
                + "        base.CommandBindings();\n"
                + "        CommandBinder.Bind(_signals.Incoming.OpenGameplayScreen).ToSequence<OpenGameplayScreenCommand>();\n"
                + "    }\n"
                + "}",
                "GameplayModule/zScreenModules/GameplayScreenModule/Scripts/Runtime/RootsContexts/GameplayScreenContext.cs");

            painter.SubHeading("Opened from a Command");
            painter.Paragraph(
                "A screen opens from a Command, never from a Mediator, so the opening is a step in a "
                + "flow the console shows. Open returns a builder and nothing happens until Show; Show "
                + "can answer null - a full layer, an unknown config, a failed load - and the await can "
                + "throw, so a retained Command resolves all three ways out:");
            painter.Code(
                "public override async void Execute()\n"
                + "{\n"
                + "    Retain();\n"
                + "\n"
                + "    try\n"
                + "    {\n"
                + "        GameplayScreenView screen = await _screenService.Open<GameplayScreenView>().Show<GameplayScreenView>();\n"
                + "\n"
                + "        if (screen == null)\n"
                + "        {\n"
                + "            FlowLogger.LogError(\"OpenGameplayScreenCommand - the screen did not open.\");\n"
                + "            Stop();\n"
                + "            return;\n"
                + "        }\n"
                + "\n"
                + "        Release();\n"
                + "    }\n"
                + "    catch (Exception exception)\n"
                + "    {\n"
                + "        FlowLogger.LogError($\"OpenGameplayScreenCommand threw while opening the screen: {exception}\");\n"
                + "        Stop();\n"
                + "    }\n"
                + "}",
                "OpenGameplayScreenCommand - the three ways out of a retained Command");

            painter.SubHeading("Filled by the Command that opens it");
            painter.Paragraph(
                "The gameplay screen has nothing to show yet, so its Command only opens it. A screen that "
                + "shows data is filled by the same Command: it reads the data where it is published - "
                + "the module's own Model, or a Shared asset through ISharedDataModel - and calls the "
                + "view's own methods on the instance it awaited, before releasing. A signal dispatched "
                + "instead would land after the screen is up, a frame of an empty screen.");
            painter.Code(
                "internal class OpenShopScreenCommand : Command\n"
                + "{\n"
                + "    [Inject] private IScreenService   _screenService { get; set; }\n"
                + "    [Inject] private ISharedDataModel _sharedData    { get; set; }\n"
                + "\n"
                + "    public override async void Execute()\n"
                + "    {\n"
                + "        Retain();\n"
                + "\n"
                + "        try\n"
                + "        {\n"
                + "            CD_Shop shop = _sharedData.GetScriptable<CD_Shop>();\n"
                + "            PD_Player player = _sharedData.GetScriptable<PD_Player>();\n"
                + "\n"
                + "            if (shop == null || player == null)\n"
                + "            {\n"
                + "                Stop();                  // the shared data model has reported the missing filing\n"
                + "                return;\n"
                + "            }\n"
                + "\n"
                + "            ShopScreenView screen = await _screenService.Open<ShopScreenView>().Show<ShopScreenView>();\n"
                + "\n"
                + "            if (screen == null)\n"
                + "            {\n"
                + "                FlowLogger.LogError(\"OpenShopScreenCommand - the screen did not open.\");\n"
                + "                Stop();\n"
                + "                return;\n"
                + "            }\n"
                + "\n"
                + "            screen.ShowItems(shop.Items);\n"
                + "            screen.ShowCoins(player.Coins);\n"
                + "            Release();\n"
                + "        }\n"
                + "        catch (Exception exception)\n"
                + "        {\n"
                + "            FlowLogger.LogError($\"OpenShopScreenCommand threw while opening the screen: {exception}\");\n"
                + "            Stop();\n"
                + "        }\n"
                + "    }\n"
                + "}",
                "Reading the published data and filling the view before anybody sees it");
            painter.Paragraph(
                "The screen module references the Shared assembly the assets live in, and nothing else of "
                + "the modules that publish them. BotBar's OpenBotBarScreenCommand is a working one: it "
                + "reads CD_BotBar and RD_BotBar and fills the bar, and refills a bar that is already up.");
            painter.PageLink("BotBar Module", "Read: BotBar Module - a screen opened and filled from shared data");
            painter.PageLink("Controllers", "Read: Controllers - Retain, Release and Stop");

            painter.SubHeading("Where a screen module goes");
            painter.Bullet(
                "In the zScreenModules of the module whose feature it shows - MainScreenModule under "
                + "MainModule, GameplayScreenModule under GameplayModule - never under another screen "
                + "module or a test module.");
            painter.Bullet(
                "Create Module with the Screen type writes the whole shape: context, view, mediator, "
                + "the prefab, its Addressables entry, and a test scene with a ScreenManager in it where "
                + "the screen is opened from code - so the screen is seen on a canvas as the game will "
                + "show it, and edited there.");
            painter.Bullet(
                "The view wires its buttons in OnEnable and unwires them in OnDisable, because the "
                + "instance is pooled and opens many times; the mediator subscribes the same way.");
            painter.PageLink("Create Module", "Open: Create Module");
            painter.PageLink("View & Mediator", "Read: View & Mediator");
            painter.PageLink("Screen Scanner", "Read: Screen Scanner - every screen and the layer it sits on");
        }
    }
}

#endif