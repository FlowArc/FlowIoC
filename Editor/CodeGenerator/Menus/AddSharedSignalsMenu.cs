#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration;
using FlowIoC.Editor.Inspector;
using FlowIoC.Editor.ModuleScanner;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus
{
    /// <summary>
    /// Every module against the two assemblies it may carve out of itself: Shared for the data it
    /// publishes, Signals for the holder other modules talk to it through.
    ///
    /// Both are ticks in Create Module, which means a module is only ever offered them on the day
    /// it is made - and the need usually arrives later, when a second module turns out to want its
    /// data or a Connector turns out to want its signals. This window closes that gap.
    ///
    /// It replaces Add Shared Data, which asked the same question about one of the two and could
    /// not say which modules the answer was yes for: it listed every module alike, so the reader
    /// had to know already. Here each row says what the module has, and offers only what it has
    /// not - which is also why this is not a column in Module Scanner. A module with no Shared
    /// assembly is not broken, and the panel that reports what is wrong hides, under "Only
    /// issues", exactly the modules this window is about.
    /// </summary>
    internal class AddSharedSignalsMenu : EditorWindow
    {
        private const string TITLE = "Add Shared or Signals";

        private const float ICON_WIDTH = 16f;
        private const float NAME_WIDTH = 220f;
        private const float CELL_WIDTH = 210f;
        private const float BUTTON_WIDTH = 96f;

        /// <summary>How far a row is sunk when the panel has nothing to offer it.</summary>
        private const float GREY_ALPHA = 0.14f;

        [MenuItem("Tools/FlowIoC/" + TITLE, false, -1294)]
        internal static void Open()
        {
            var window = GetWindow<AddSharedSignalsMenu>(TITLE);
            window.minSize = new Vector2(660, 420);
            window.Show();
        }

        private readonly FlowRowPainter _painter = new FlowRowPainter();
        private readonly ModuleOffers _offers = new ModuleOffers();

        private FlowHeaderBar _bar;
        private List<ModuleTargetEVO> _modules;
        private Vector2 _scroll;
        private string _lastResult;

        private void OnEnable()
        {
            titleContent = new GUIContent(TITLE);

            // Without this the window is sent no MouseMove events at all, and a row would only
            // light up when something else happened to repaint it.
            wantsMouseMove = true;

            _bar = new FlowHeaderBar(new FlowPalette(), new FlowHelpPageMap());

            Rescan();
        }

        private void OnFocus() => Refresh();

        /// <summary>
        /// A rescan that also drops the last install's account of itself. That account describes
        /// one press of one button, and it has nothing to say the next time somebody comes back.
        /// </summary>
        private void Refresh()
        {
            _lastResult = string.Empty;
            Rescan();
        }

        private void Rescan()
        {
            (ProjectTargetEVO _, List<ModuleTargetEVO> modules) = new ModuleTargetFactory().Build();

            _modules = modules;
            Repaint();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            // FlowIoC's own colour, through the overload the generator windows use. This is one of
            // them: it writes files into a module the way Create Module does, so it belongs with
            // Create Module and Create Command rather than with the scanners. The rows underneath
            // stay green, because green is what a settled row is everywhere in FlowIoC.
            _bar.DrawWindow(
                TITLE, "FlowIoC", "What a module publishes, and what it announces",
                "Refresh", Refresh, "Creating a Module");

            EditorGUILayout.HelpBox(
                "Shared is the data a module publishes: Scripts/Shared, an assembly of its own, and "
                + "the references that let the module's screen, sub and test modules read it.\n\n"
                + "Signals is the module's public surface: the holder in Scripts/Signals, its "
                + "assembly, and the binding that puts it in the module's Context. The folder is on "
                + "every module already - what a module created without signals lacks is the holder "
                + "inside it.\n\n"
                + "A test module and a Connector are offered neither, and their rows say why: a test module "
                + "publishes and announces nothing, and a Connector wires other modules rather than owning "
                + "signals or data of its own.",
                MessageType.Info);

            DrawList();

            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
        }

        private void DrawList()
        {
            DrawHeadings();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_modules == null || _modules.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No modules found. A module is a folder whose name ends in \"Module\", under "
                    + "Assets/Modules or inside an embedded package.",
                    MessageType.Info);
            }
            else
            {
                foreach (ModuleTargetEVO module in _modules)
                    DrawModuleRow(module);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeadings()
        {
            Rect rect = _painter.Row(FlowRowPainter.ROW_HEIGHT);
            _painter.Paint(rect, _painter.Ok, FlowRowPainter.HEADING_ALPHA);

            float x = rect.x + _painter.ContentX;

            // All three in the one column style: they name the three columns, so a reader should
            // not have to work out why one of them is bold white and the other two are not.
            GUIStyle heading = _painter.Heading(_painter.Ok);

            GUI.Label(new Rect(x, rect.y, NAME_WIDTH, rect.height), "MODULE", heading);
            x += NAME_WIDTH + 6f;

            GUI.Label(new Rect(x, rect.y, CELL_WIDTH, rect.height), "SHARED", heading);
            x += CELL_WIDTH + 6f;

            GUI.Label(new Rect(x, rect.y, CELL_WIDTH, rect.height), "SIGNALS", heading);
        }

        /// <summary>
        /// One module, and what it has of the two. A module that is offered neither is still
        /// listed - the list is the project's modules, not a shopping basket - but it is drawn
        /// grey and says why, so a reader is not left wondering where its buttons went.
        ///
        /// The row itself takes no click: only the two buttons do anything. So it does not light
        /// up under the pointer either, because a highlight on a row that answers a click with
        /// nothing is a promise the panel cannot keep.
        /// </summary>
        private void DrawModuleRow(ModuleTargetEVO module)
        {
            string notOffered = _offers.WhyNotOffered(module);
            IReadOnlyList<ModuleOfferEVO> offers = _offers.For(module);

            Rect rect = _painter.Row();

            // Every row that is offered something wears the settled green, not the amber a scanner
            // row would. Amber says a reader has to act, and nothing here is owed: a module with
            // no Shared assembly is finished as it stands. The buttons are what says an offer is
            // available, and a button is louder than a tint anyway.
            if (notOffered != null) _painter.Darken(rect, GREY_ALPHA);
            else _painter.Paint(rect, _painter.Ok, FlowRowPainter.QUIET_ALPHA);

            float x = rect.x + _painter.ContentX;

            GUI.Label(new Rect(x, rect.y, NAME_WIDTH, rect.height), module.Name,
                notOffered != null ? _painter.Mini(false) : _painter.Name(false));
            x += NAME_WIDTH + 6f;

            if (notOffered != null)
            {
                GUI.Label(new Rect(x, rect.y, rect.xMax - x - 6f, rect.height), notOffered, _painter.Mini(false));
                return;
            }

            DrawCell(new Rect(x, rect.y, CELL_WIDTH, rect.height), module, offers, ModuleOfferKind.Shared);
            x += CELL_WIDTH + 6f;

            DrawCell(new Rect(x, rect.y, CELL_WIDTH, rect.height), module, offers, ModuleOfferKind.Signals);
        }

        /// <summary>
        /// A button while the module has not got it, and what it has otherwise. A test module is
        /// offered nothing and holds nothing, so its cells say so rather than sitting blank.
        /// </summary>
        private void DrawCell(
            Rect rect, ModuleTargetEVO module, IReadOnlyList<ModuleOfferEVO> offers, ModuleOfferKind kind)
        {
            ModuleOfferEVO offer = Offer(offers, kind);

            if (offer != null)
            {
                var button = new Rect(rect.x, rect.y + 1f, BUTTON_WIDTH, rect.height - 2f);

                if (GUI.Button(button, offer.Label, EditorStyles.miniButton))
                    Apply(offer, module);

                return;
            }

            string assembly = _offers.AssemblyOf(module, kind);

            Color previous = GUI.color;
            GUI.color = _painter.Ok;
            GUI.Label(new Rect(rect.x, rect.y, ICON_WIDTH, rect.height), "✔", _painter.Icon);
            GUI.color = previous;

            GUI.Label(new Rect(rect.x + ICON_WIDTH + 2f, rect.y, rect.width - ICON_WIDTH - 2f, rect.height),
                string.IsNullOrEmpty(assembly) ? "in place" : assembly, _painter.Mini(false));
        }

        private ModuleOfferEVO Offer(IReadOnlyList<ModuleOfferEVO> offers, ModuleOfferKind kind)
        {
            foreach (ModuleOfferEVO offer in offers)
            {
                if (offer.Kind == kind) return offer;
            }

            return null;
        }

        private void Apply(ModuleOfferEVO offer, ModuleTargetEVO module)
        {
            ModuleInstallReport report = _offers.Apply(offer, module);

            report.Log(module.Name);
            _lastResult = report.Succeeded ? report.Summary() : report.Error;

            AssetDatabase.Refresh();
            Rescan();
        }
    }
}

#endif