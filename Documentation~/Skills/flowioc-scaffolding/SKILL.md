---
name: flowioc-scaffolding
description: Use when creating, extending or deleting a module in a FlowIoC Unity project - a new main, screen or test module, a Shared or Signals assembly on a module that already exists, which folder a module's public and internal signal holder each lives in and which of the two carries Incoming and Outgoing, generating the files for a Model or a View, or when namespaces and .csproj.DotSettings need rebuilding after any of that.
---

# FlowIoC Scaffolding

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Never write a module by hand

A FlowIoC module is more than its folders. Creating one also writes an asmdef, writes a
`<Assembly>.csproj.DotSettings` at the **project root**, registers the module in the module index,
and writes its `FlowModule` part. Typing the folders out by hand produces the first part and
silently skips the rest, which shows up later as a namespace with `Scripts` in the middle of it, a
module the generators cannot find, or `FlowModule.PlayerModule` failing to compile.

Use the menu items below instead. They are the only supported way to lay a module out, and the
code generators and namespace tools depend on the exact shape they produce.

## The tools

| Menu item | What it does |
|---|---|
| `Tools/FlowIoC/Create New Module` | A whole module: folders, asmdef, Root, Context, Signals, DotSettings, index entry, log channel |
| `Tools/FlowIoC/Edit Module/Add Shared or Signals` | Adds the `Scripts/Shared/` assembly to a module that already exists and points its screen, sub and test modules at it, or writes the public signal holder a module was created without |
| `Tools/FlowIoC/Edit Module/Create Command` | One Command, in `Controllers/`, in the module's namespace |
| `Tools/FlowIoC/Edit Module/Create Function` | One Function, in `Controllers/` beside the Commands, on the arity its parameters and return type imply |
| `Tools/FlowIoC/Edit Module/Create Model` | An interface and an implementation, in `Models/` |
| `Tools/FlowIoC/Edit Module/Create View` | A View and its Mediator, in `ViewsMediators/` |
| `Tools/FlowIoC/Module Scanner` | Reports every module's folders, assemblies, references and namespace settings, and repairs what is safe to repair |
| `Tools/FlowIoC/Edit Module/Delete Module` | Removes the folder, asmdef, DotSettings, csproj, index entry and log channel together |
| `Tools/FlowIoC/Edit Module/Rename Module` | Renames a module and carries the name to its assemblies and their references, namespaces, DotSettings, log channel, the Root, Context and signal holders named after it, a screen's prefab and address, and the Roots that list its contexts. Never rename a module folder in the Project window |

## Create Module: what to fill in

**Name** excludes the suffix. The generator appends it, so the window's preview and a call from a
script agree: `Player` with type Main becomes `PlayerModule`, with type Test becomes
`PlayerTestModule`, with type Screen becomes `PlayerScreenModule`. A name that already ends in
`Test` or `Screen` is left as it is.

**Module Type** decides where it lands. A Main module parented to `Assets/Modules` is a top level
module; the same type parented to another module makes it a sub module under `zSubModules`. Test
and Screen modules go under `zTestModules` and `zScreenModules` of the module you pick as parent.

**Parent Module** is `Assets/Modules` for a top level module, and the owning module for anything
else.

**Role** names the Root and the Context for what the Root roots, and the inspector reads that name
to colour them. **System** writes `PlayerSystemRoot` and `PlayerSystemContext` and is what the
dropdown starts on, because a module written for the game at hand is a System. **Service** writes
`CounterServiceRoot` and `CounterServiceContext`.

**Core** writes the plain `PlayerRoot` and `PlayerContext`, and adds `[FlowHeader(FlowRole.Core)]`
above the Root. It is the one role a name cannot carry: a Core module is part of the project's frame
rather than of its game - the project holds exactly one, its Initialize Order is reserved rather
than chosen, and a game extends it instead of authoring it, the way a screen module is listed on a
Root or a Connector sub-context is added to the Connector. `MainRoot` and `ScreenRoot` are what it
means. There being one Main and one Screen in a project, the name has nothing to disambiguate from
and takes no suffix, so the attribute is what tells the inspector.

The role also ticks the folder it implies in the structure panel: System ticks `Systems/`, Service
ticks `Services/`, and Core ticks neither. The tick stays editable, so a module that wants both says
so before pressing Create.

The module folder, its assembly and its namespaces are untouched whichever is picked. Role is
offered on a main module that gets a Root; a screen or test module's Root is none of the three, so
none is asked.

### Optional folders

Only the mandatory folders, Signals, and whatever Role ticked are created unless you tick more.
Decide the rest before pressing Create:

| Folder | Tick it when |
|---|---|
| `Services` | ticked for you by **Role = Service**. The module has an interface and implementation that answer the input they are given, and that other modules may inject |
| `Systems` | ticked for you by **Role = System**. The module wants a surface - one injection instead of eight, or a chained list of what is available. A System needs no `System.cs`, so untick it for a module whose work is followed through its Context's command flow |
| `Constants`, `Enums` | the module has either |
| `Scenes`, `Resources`, `Art`, `Scriptables` | the module owns assets of that kind |
| `Shared` | the module publishes data other modules read. Starts unticked. The public signal holder is **not** in here - that lives in `Scripts/Signals/`, which is a tick of its own |
| `Signals` | the module has a public surface. Starts **ticked**, forced on for a screen module, not offered to a test module. Untick it for a Service that answers its caller rather than announcing, and for a Connector, which owns no signals at all |
| `zSubModules`, `zTestModules`, `zScreenModules` | other modules will hang under this one |

### Adding a folder afterwards

Nothing has to be deleted. Run Create Module again with the same name and the same parent, tick
the folders that are missing, and untick **Create Root**, **Create Context** and **Create
Signals**. Existing files are left alone; the missing folders appear and the module index is
refreshed so the other generators can find them.

For `Shared` specifically, prefer `Tools/FlowIoC/Edit Module/Add Shared or Signals` - it also adds the reference
to every screen, sub and test module already under the module, and the same window writes a public
signal holder for a module created without one.

## The two signal holders, and which folder each lives in

A module has two, and they are not the same kind of thing.

**The public holder** goes in `Scripts/Signals/`, an assembly of its own - `Modules.Player.Signals`,
beside `Modules.Player` and `Modules.Player.Shared`. It is the module's public surface:
`PlayerSignals`, with nested `PlayerSignalsIncoming` and `PlayerSignalsOutgoing`, because those two
halves are what a boundary is made of. Only a Connector references it - and the module's own test
module, which may reference anything.

**The folder is a choice, ticked by default.** Most modules have a public surface, and the ones that
do not are worth naming: a Service that answers the caller it was given rather than announcing
anything, and a Connector, which wires other modules' signals and owns none. Untick it and the
module never has a `Scripts/Signals/` at all - no folder, no assembly, and no tool asking about
either. A screen module is the exception and cannot untick it: it generates no Context of its own,
so the holder is the only way anything reaches it.

Where the folder is there, everything downstream of it is checked: the assembly inside it, its
`.csproj.DotSettings`, and the reference from it to the module's own `Shared` assembly, which a
public signal generic over a published type needs. Where it is not, none of that is asked and
nothing puts the folder back.

**The internal holder** goes in `Scripts/Runtime/Signals/`, inside the module's own assembly, and is
named `PlayerInternalSignals` - the `XInternalSignals` form, not `XSignalsInternal`:

```csharp
internal class PlayerInternalSignals : ISignalHolder
{
    public Signal Tick = new(hideCommandLog: true);
    public Signal<double> RecalculateInterest = new();
}
```

It has **no `Incoming` and no `Outgoing`**. Those halves say what a module accepts and what it
announces across a boundary, and an internal signal never crosses one - it is the module talking to
its own commands. So it is a flat list. It is `internal` as well, so nothing outside the module's
assembly can dispatch it, which is what the two folders are really buying: the compiler decides
which signals are public rather than the reader's memory.

*Tools ▸ FlowIoC ▸ Edit Module ▸ Add Shared or Signals* writes the public holder for a module created without one.
The internal holder is an ordinary file you add when the module first needs to talk to itself.

## Where the DotSettings go

`<Assembly>.csproj.DotSettings` is written to the **project root**, beside the `.csproj` Unity
generates - not inside the module. Rider only reads it from there. A module gets one file per
assembly it has - its own, its `Scripts/Signals/` one, and its `Scripts/Shared/` one when it has
Shared - because a `.csproj.DotSettings` applies solely to the project it is named after, and the
module's own file cannot tell Rider to skip `Scripts` on the others' behalf.

These files are generated. After moving a module, renaming a folder, or editing the folder layout
in the code generator settings, run `Module Scanner` rather than editing them - it
rewrites all of them and clears out the ones whose module is gone.

## Driving the tools without clicking

When a Unity Editor is open on the project, the tools can be run from a terminal instead of the
Editor UI. Find the Editor and then execute code inside it:

```bash
unity status                                  # look for state "ready"
unity command eval 'return UnityEngine.Application.dataPath;'
```

`Create Module` is a window, so the useful entry point is the generator underneath it. Everything
in `FlowIoC.Editor` is `internal`, so reach it by reflection. Creating the window instance runs its
`OnEnable`, which builds the folder config map and the default selections for you:

```csharp
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var asm = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(),
    a => a.GetName().Name == "FlowIoC.Editor");

var menuType = asm.GetType("FlowIoC.Editor.CodeGenerator.Menus.Module.CreateModule.CreateModuleMenu");
var genType  = asm.GetType("FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration.ModuleGenerator");
var modType  = asm.GetType("FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleType");
var roleType = asm.GetType("FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleRole");
var cardType = asm.GetType("FlowIoC.Editor.ModuleCards.ModuleCardDraftEVO");

var win = UnityEngine.ScriptableObject.CreateInstance(menuType);   // OnEnable fills the defaults
var configMap   = menuType.GetField("_directoryConfigMap", flags).GetValue(win);
var actionNames = menuType.GetField("_actionNames", flags).GetValue(win);
var selected    = menuType.GetField("_selectedOptionalFolders", flags).GetValue(win);
// add the FolderEVO entries you want out of configMap's RootFolders before calling

// The card's two authored lines, written into MODULE.md as the module is made. Leave one empty
// and the stub's placeholder goes in instead, and Module Scanner reports it until it is written.
var card = System.Activator.CreateInstance(cardType, true);
cardType.GetProperty("Purpose",  flags).SetValue(card, "Owns the player's currency and level.");
cardType.GetProperty("Concepts", flags).SetValue(card, "player, currency, level, IPlayerModel");

genType.GetMethod("CreateModuleStructure",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
    .Invoke(null, new object[] {
        "Player",                                          // name, without "Module" and without the type's suffix
        System.IO.Path.Combine(UnityEngine.Application.dataPath, "Modules"),   // Assets/Modules, or the owning module's folder
        System.Enum.Parse(modType, "Main"),                // Main, Test or Screen
        selected, configMap, actionNames,
        true, true, true, false,                           // createRoot, createContext, createSignals, createScreen
        false,                                             // allowAsSubContext
        System.Enum.Parse(roleType, "System"),             // System, Service or Core
        null,                                              // screenSettings: a screen's ScreenModuleSettings, else null
        card                                               // ModuleCardDraftEVO, or null for the placeholders
    });
```

All fourteen arguments go in: `Invoke` fills no optional parameter on its own, and a shorter list
is a `TargetParameterCountException` before anything runs.

**The name is the bare one, for a Test or a Screen module too.** The generator appends the type's
suffix - `"Player"` with type Test is `PlayerTestModule`, with type Screen `PlayerScreenModule` -
and leaves a name that already carries it alone. The suffix used to be the window's to add, and a
call from here that named a test module `"Ads"` wrote `zTestModules/AdsModule` with the parent's
own assembly name, and its namespace settings over the parent's. Read the folder name and the
asmdef name back after the call.

Three things to know before relying on this:

**`eval` gives up on the response after about five seconds of main thread work, but the code
usually finishes anyway.** Module generation takes longer than that, so a timeout is the normal
result rather than a failure. Verify by looking at the project - the module folder, the asmdef, the
`.csproj.DotSettings` at the root - not by trusting the error.

**Compilation needs asking for.** After writing files, `AssetDatabase.Refresh()` then
`CompilationPipeline.RequestScriptCompilation()`, and poll `recompile_status` until it reports
`completed`. A stale `up_to_date` usually means the refresh has not landed yet.

**The log channel is written from the module index.** Create Module writes the part as soon as the
index knows the module; a module folder that arrived another way - copied in, or written by hand -
has no part until the index is rebuilt and the generator runs. Force both:

```csharp
asm.GetType("FlowIoC.Editor.CodeGenerator.Detector.ModuleAutoDetector")
   .GetMethod("RescanModules", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
   .Invoke(null, null);
asm.GetType("FlowIoC.Editor.Console.FlowModuleGenerator")
   .GetMethod("Generate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
   .Invoke(null, null);
```

`ModuleDeleter.DeleteModule` opens no dialog and returns the list of what it removed, so it is safe
to call this way too. Anything else that ends in `EditorUtility.DisplayDialog` is not: a modal
blocks the Editor and the connection with it until somebody clicks.

## Before you say the module is done

"Is anything left?" is answered by the Module Scanner, not from memory. It is the checklist -
folders, assembly, references, namespace settings, index entry, log channel and card, for every
module in the project - and the window and the warning the Editor prints on load (`Module Scanner
found N issues across M modules`) both come from one runner, reachable from `eval` without opening
anything:

```csharp
var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var asm = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "FlowIoC.Editor");
System.Func<object, string, object> prop = (o, n) => o.GetType().GetProperty(n, f).GetValue(o);

var factory  = System.Activator.CreateInstance(asm.GetType("FlowIoC.Editor.ModuleScanner.ModuleTargetFactory"), true);
var built    = factory.GetType().GetMethod("Build", f).Invoke(factory, null);
var pipeline = System.Activator.CreateInstance(asm.GetType("FlowIoC.Editor.ModuleScanner.ModuleCheckPipeline"), true);
var runner   = System.Activator.CreateInstance(asm.GetType("FlowIoC.Editor.ModuleScanner.ModuleScannerRunner"), f, null, new[] {pipeline}, null);
var report   = runner.GetType().GetMethod("Run", f).Invoke(runner,
    new[] {built.GetType().GetField("Item1").GetValue(built), built.GetType().GetField("Item2").GetValue(built)});

var lines = new System.Text.StringBuilder("Issues=" + prop(report, "IssueCount") + "\n");
foreach (var finding in (System.Collections.IEnumerable) prop(report, "Project"))
    if (prop(finding, "Status").ToString() != "Ok") lines.AppendLine("  project: " + prop(finding, "Message"));
foreach (var row in (System.Collections.IEnumerable) prop(report, "Modules"))
foreach (var finding in (System.Collections.IEnumerable) prop(row, "Findings"))
    if (prop(finding, "Status").ToString() != "Ok")
        lines.AppendLine("  " + prop(row, "Name") + " [" + prop(finding, "Status") + "] " + prop(finding, "Message"));
return lines.ToString();
```

`Fixable` is what the window's Fix All repairs: a mandatory folder missing, namespace settings
stale, an orphaned `.csproj` at the root. `Manual` is a decision: a card with its purpose
unwritten, a module reaching another module's Signals assembly. Report both as they stand - fix
what the task covers, name the rest - and never answer "nothing left" without having run this.

Then, for the module you just made:

- **The card.** `MODULE.md` above the generated block: Purpose and Concepts written - the scanner
  reports the placeholders - and Decisions and Known gaps where there is anything to say. Pass
  `card` at creation or write it straight after; it is part of making the module, not a follow-up
  for somebody to ask for.
- **The name on disk.** `<Name>Module`, `<Name>TestModule` or `<Name>ScreenModule`, and the asmdef
  inside it `Modules.<Name>`, `Modules.<Name>.Test` or `Modules.<Name>.Screen`. A wrong name is
  not put right by renaming the folder: `ModuleDeleter.DeleteModule`, then create again, because a
  folder deleted by hand leaves its `.csproj` and `.csproj.DotSettings` at the project root.
- **The index.** `Assets/Plugins/FlowIoC/MODULES.md` lists the module with its purpose and its
  concepts after the next compile.
- **A screen's test scene.** `<Name>ScreenTestScene` under the screen's test module, with the
  prefab on its layer - written by the half that runs after the reload.
- **A test module of your own.** Made through the generator too, so it carries the mandatory
  folders; a test module copied or written by hand is the one the scanner finds a folder short.

## A panel a module ships

A module that wants a window for the developer - the save module shows the save file on this
machine and resets it - ships a panel. It is one class in `Scripts/Editor/`, wrapped in
`#if UNITY_EDITOR`, deriving from `ModulePanel` in `FlowIoC.Editor.ModulePanels`, and the module's
asmdef references `FlowIoC.Editor` for it; Unity drops that reference when it builds a player, so
the runtime assembly is unchanged. The framework draws the window - the bar in the module's role
colour, the rows through `ModulePanelPainter`'s marks - and the module declares what is on it:

```csharp
#if UNITY_EDITOR
internal class LocalSavePanel : ModulePanel
{
    [MenuItem("Tools/FlowIoC-Modules/Local Save/Panel", false, -1080)]
    private static void Open() => ModulePanelWindow.Open<LocalSavePanel>();

    public override string Title => "Local Save";
    public override string Module => "LocalSaveModule";
    public override FlowRole Role => FlowRole.Service;
    public override string HelpPage => "Local Save";

    public override void Draw(ModulePanelPainter painter)
    {
        painter.Heading("File");
        painter.Field("Path", _tools.Path);
        painter.Actions(new ModulePanelAction("Reset save", ResetSave, enabled: !EditorApplication.isPlaying, destructive: true));
    }
}
#endif
```

The menu root is `Tools/FlowIoC-Modules/<Module>/`, never `Tools/FlowIoC` - the framework's menu
is the framework's. Priority `-1080` is what keeps that root second under Tools, between
FlowIoC and FlowIoC-dev. A menu path cannot carry a slash, so a module called A/B Test names its
entry `AB Test`. There is no generator for a panel: it is one file.

A panel reads files and prefs and resets them; it never edits a Model's values. A value changed
from a panel skips the rules the Model keeps, and the file is the Model's. Reading the save file,
deleting it, clearing PlayerPrefs, writing a pref in the module's own format so the next run
reads it - all inside the line. A text field that writes into a loaded Model is not.

A panel that authors an asset - the A/B Test Editor over `CD_AbTests` - draws the asset's own
`SerializedProperty`s through `painter.Property(property, label)` and `painter.Properties(label,
a, b)`, so undo, dirtying and saving are Unity's; it edits what the Inspector would edit, which
is inside the line. Defer a change to a list's shape - an element added or removed - until the
rows are drawn, because the rows are walked by index.

Put the file operations in a class of their own beside the panel (`LocalSaveFileTools`) so the
workspace's tests reach them without a window; the panel only draws.

## After the module exists

`Create Command`, `Create Function`, `Create Model` and `Create View` place their files in the right folder and
namespace on their own. Prefer them over writing the files by hand, for the same reason as the
module itself.

The architecture rules the generated module is shaped around - what a Command may do, why a Model
never subscribes to a signal, how two modules meet in a Connector - live in `AGENTS.md` at the
project root. Read that before filling the module in.
