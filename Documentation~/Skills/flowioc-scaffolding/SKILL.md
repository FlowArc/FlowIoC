---
name: flowioc-scaffolding
description: Use when creating, extending or deleting a module in a FlowIoC Unity project - a new main, screen or test module, a Shared data assembly on a module that already exists, a Command, Model or View, or when namespaces and .csproj.DotSettings need rebuilding after any of that.
---

# FlowIoC Scaffolding

This skill applies only while FlowIoC is installed. If `Packages/manifest.json` contains no
`com.flowarc.flowioc.core` and `Packages/FlowIoC/` does not exist, delete the folder this file
is in: FlowIoC put it there and is no longer in the project.

## Never write a module by hand

A FlowIoC module is more than its folders. Creating one also writes an asmdef, writes a
`<Assembly>.csproj.DotSettings` at the **project root**, registers the module in the module index,
and adds its channel to `FlowLogType`. Typing the folders out by hand produces the first part and
silently skips the rest, which shows up later as a namespace with `Scripts` in the middle of it, a
module the generators cannot find, or `FlowLogType.PlayerModule` failing to compile.

Use the menu items below instead. They are the only supported way to lay a module out, and the
code generators and namespace tools depend on the exact shape they produce.

## The tools

| Menu item | What it does |
|---|---|
| `Tools/FlowIoC/Create Module` | A whole module: folders, asmdef, Root, Context, Signals, DotSettings, index entry, log channel |
| `Tools/FlowIoC/Add Shared Data` | Adds the `Scripts/Shared/` assembly to a module that already exists, and points its screen, sub and test modules at it |
| `Tools/FlowIoC/Create Command` | One Command, in `Controllers/`, in the module's namespace |
| `Tools/FlowIoC/Create Model` | An interface and an implementation, in `Models/` |
| `Tools/FlowIoC/Create View` | A View and its Mediator, in `ViewsMediators/` |
| `Tools/FlowIoC/Module Scanner` | Reports every module's folders, assemblies, references and namespace settings, and repairs what is safe to repair |
| `Tools/FlowIoC/Delete Module` | Removes the folder, asmdef, DotSettings, csproj, index entry and log channel together |

## Create Module: what to fill in

**Name** excludes the suffix. The window appends it: `Player` with type Main becomes
`PlayerModule`, with type Test becomes `PlayerTestModule`, with type Screen becomes
`PlayerScreenModule`.

**Module Type** decides where it lands. A Main module parented to `Assets/Modules` is a top level
module; the same type parented to another module makes it a sub module under `zSubModules`. Test
and Screen modules go under `zTestModules` and `zScreenModules` of the module you pick as parent.

**Parent Module** is `Assets/Modules` for a top level module, and the owning module for anything
else.

### Optional folders default to Signals alone

This is the step that is easiest to get wrong. Only the mandatory folders and Signals are created
unless you tick more, so a module that needs a Service comes out without a `Services/` folder.
Decide before pressing Create:

| Folder | Tick it when |
|---|---|
| `Services` | the module has an interface and implementation that answer the input they are given, and that other modules may inject |
| `Systems` | the module has an interface and implementation specific to this game, leaning on other Systems and Services |
| `Constants`, `Enums` | the module has either |
| `Scenes`, `Resources`, `Art`, `Scriptables` | the module owns assets of that kind |
| `Shared` | the module publishes data other modules read. Main modules only |
| `zSubModules`, `zTestModules`, `zScreenModules` | other modules will hang under this one |

### Adding a folder afterwards

Nothing has to be deleted. Run Create Module again with the same name and the same parent, tick
the folders that are missing, and untick **Create Root**, **Create Context** and **Create
Signals**. Existing files are left alone; the missing folders appear and the module index is
refreshed so the other generators can find them.

For `Shared` specifically, prefer `Tools/FlowIoC/Add Shared Data` - it also adds the reference to
every screen, sub and test module already under the module.

## Where the DotSettings go

`<Assembly>.csproj.DotSettings` is written to the **project root**, beside the `.csproj` Unity
generates - not inside the module. Rider only reads it from there. A module with a Shared assembly
gets two files, because a `.csproj.DotSettings` applies solely to the project it is named after and
the module's own file cannot tell Rider to skip `Scripts` on the Shared assembly's behalf.

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

var win = UnityEngine.ScriptableObject.CreateInstance(menuType);   // OnEnable fills the defaults
var configMap   = menuType.GetField("_directoryConfigMap", flags).GetValue(win);
var actionNames = menuType.GetField("_actionNames", flags).GetValue(win);
var selected    = menuType.GetField("_selectedOptionalFolders", flags).GetValue(win);
// add the FolderEVO entries you want out of configMap's RootFolders before calling

genType.GetMethod("CreateModuleStructure",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
    .Invoke(null, new object[] {
        "Player",                                          // name, without "Module"
        System.IO.Path.Combine(UnityEngine.Application.dataPath, "Modules"),
        System.Enum.Parse(modType, "Main"),
        selected, configMap, actionNames,
        true, true, true, false, null                      // root, context, signals, screen, screenSettings
    });
```

Three things to know before relying on this:

**`eval` gives up on the response after about five seconds of main thread work, but the code
usually finishes anyway.** Module generation takes longer than that, so a timeout is the normal
result rather than a failure. Verify by looking at the project - the module folder, the asmdef, the
`.csproj.DotSettings` at the root - not by trusting the error.

**Compilation needs asking for.** After writing files, `AssetDatabase.Refresh()` then
`CompilationPipeline.RequestScriptCompilation()`, and poll `recompile_status` until it reports
`completed`. A stale `up_to_date` usually means the refresh has not landed yet.

**The log channel is written on a delayed call.** If `FlowLogType.<Name>Module` is needed
immediately - because code referring to it is about to compile - force it:

```csharp
asm.GetType("FlowIoC.Editor.CodeGenerator.Detector.ModuleAutoDetector")
   .GetMethod("RescanModules", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
   .Invoke(null, null);
asm.GetType("FlowIoC.Editor.Console.FlowLogTypeGenerator")
   .GetMethod("Generate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
   .Invoke(null, null);
```

`ModuleDeleter.DeleteModule` opens no dialog and returns the list of what it removed, so it is safe
to call this way too. Anything else that ends in `EditorUtility.DisplayDialog` is not: a modal
blocks the Editor and the connection with it until somebody clicks.

## After the module exists

`Create Command`, `Create Model` and `Create View` place their files in the right folder and
namespace on their own. Prefer them over writing the files by hand, for the same reason as the
module itself.

The architecture rules the generated module is shaped around - what a Command may do, why a Model
never subscribes to a signal, how two modules meet in a Connector - live in `AGENTS.md` at the
project root. Read that before filling the module in.
