# Editor Tools

FlowIoC ships a set of Unity Editor windows. Most of them exist because the framework
is convention-heavy: a module has a fixed folder shape, a Root has a fixed
relationship to its Context, and a screen has to be described in three places at once.
The tools keep those conventions correct so you do not have to.

| Menu | Use it to |
|---|---|
| `Tools/FlowIoC/Create Module` | Scaffold a whole module — folders, assembly definition, Root and Context |
| `Tools/FlowIoC/Create Command` | Add a command in the right folder and namespace |
| `Tools/FlowIoC/Create Model` | Add an `IXModel` / `XModel` pair |
| `Tools/FlowIoC/Create View` | Add a View, its Mediator and its prefab |
| `Tools/FlowIoC/Delete Module` | Remove a module and its registrations |
| `Tools/FlowIoC/Flow Console` | Watch signals, commands and contexts at runtime |
| `Tools/FlowIoC/Model Viewer` | Inspect live model state while playing |
| `Tools/FlowIoC/Folder Painter` | Colour Project window folders by path or by folder |
| `Tools/FlowIoC/Screen Scanner` | Every screen context on a Root in the open scenes, with its manager, layer, tag and animation flags editable in place |
| `Tools/FlowIoC/Module Scanner` | Report every module's folders, assemblies and namespace settings, and repair what is safe to repair |

---

## Creating Things

### Create Module

The one you use first. Pick a name and a module type:

| Type | Produces |
|---|---|
| `Main` | A normal feature module under `Assets/Modules/<Name>Module/` |
| `Screen` | A screen module: view, mediator, its context, and optional scene |
| `Test` | An isolated test module, wrapped in editor-only preprocessor directives |

It writes the folder tree, `Modules.<Name>.asmdef`, and — if you leave the toggles
on — the `Root` / `Context` pair, and registers the module in the project's module
index the moment its folder exists.

For a Screen module you can also list the screen's actions up front
(`OnBackButtonClicked`, `OnSettingsClicked`) and the generator puts them on both the
View and the Mediator.

Use this rather than copying a folder. A copied module carries the source module's
namespace and its asmdef name, and Unity refuses the resulting duplicate assembly
name rather than telling you which copy is at fault.

### Create Command / Create Model / Create View

The same idea at a smaller scale: each asks which module and which sub-module the new
class belongs to, then writes it into the right folder with the right namespace. The
View generator also creates the prefab and adds the `ViewInjector` component, which
is easy to forget by hand and produces a view that silently never registers.

---

## Keeping Things Correct

### Module Scanner

`Tools ▸ FlowIoC ▸ Module Scanner` reads every module in the project and reports what each
one is missing:

| Checks | Looks for |
|---|---|
| Mandatory folders | The folders this module type's layout says must exist |
| Shared assembly | A module with a `Scripts/Shared` folder must have the assembly that folder is for |
| Assembly definition | One asmdef at the module root, named to the module convention |
| References | Its own Shared assembly, its parent's Shared assembly, and for a test module its parent's own assembly |
| Namespace settings | The root `.csproj.DotSettings` that tells Rider which folders produce a namespace |
| The project | The module index against the folder tree, orphaned settings files, the Flow log types, the solution code style |

`Fix All` repairs everything that can be repaired without guessing. It will not rename
an assembly or remove a reference — renaming one moves every asmdef that names it and
the settings file named after it, so those rows stay red and say what to do.

Assembly definitions are what make a module's boundary real: without one, "this
module does not reference that module" is a rule nobody enforces. A module with no
assembly is also invisible to the namespace settings, which skip it silently — Module
Scan is where that gap becomes visible.

Run it after moving folders around in the Project window. The symptom that you needed
it is a generator writing into the wrong place, or a Root whose sub-context list has
gone empty. If anything is wrong, the console says so once on editor load.

### Delete Module

Removes the folder, its assembly definition and its metadata together. Deleting a
module by hand tends to leave the asmdef reference behind in other modules' asmdefs,
which fails to compile in a way that does not name the module you deleted.

---

## Looking at a Running Game

### Flow Console

The framework logs itself into this window: every signal dispatch, command step,
context phase, screen transition and pool operation, on channels you can toggle
independently. Most debugging in FlowIoC starts here rather than with a breakpoint.

See [Flow Console](../Runtime/ConsoleModule/Documentation/FlowConsole.md).

### Model Viewer

Shows the live contents of your models while the game runs. Mark what you want to see:

```csharp
public class CameraModel : ICameraModel
{
    [ShowInModelViewer] private readonly Dictionary<string, CameraCVO> _cameras = new();
    [ShowInModelViewer] private CinemachineCamera _activeCamera;

    [HideInModelViewer] private byte[] _scratchBuffer;
}
```

Because models are plain C# objects rather than `MonoBehaviour`s, the Unity Inspector
cannot show them — this window is the replacement.

### Folder Painter

Tints Project window folders so a large module tree stays readable at a glance. Two
kinds of rule:

- **Path rules** match on the folder path — *contains*, *ends with* or *starts with*.
  They are checked in order and the first match wins, so put the specific ones first.
  A rule ending with `Module` colours every generated module without naming any of them.
- **Folder rules** point at one folder asset and take priority over the path rules.
  Use them for the handful of folders you want to stand out individually.

Each rule sets a gradient, and optionally a label override, a selection colour and an
icon.

Open it from `Tools/FlowIoC/Folder Painter`. Edits repaint the Project window as you
make them.

The settings live in your project, at
`Assets/Plugins/FlowIoC/Editor/FolderPainter/FlowIoCFolderPainterConfig.asset`, not in the
package — so colours are per project, and are created with a sensible default set the
first time the Editor opens.

---

## Inspector Additions

These attributes change how a class or field appears in the Editor:

| Attribute | Effect |
|---|---|
| `[CustomClassHeader(...)]` | Colours a Root or Context header so it is recognisable in the Inspector |
| `[ShowInModelViewer]` / `[HideInModelViewer]` | Opt a member in or out of the Model Viewer |
| `[ExcludeFromContextWindow]` | Hide a Context from the *Add Sub Context* picker — use it on test contexts |
| `[ReadOnly]` | Show a field in the Inspector without allowing edits |

The Root inspector itself is generated: it shows the binding phase toggles
(`AutoInitialize`, `AutoSetup`, `AutoLaunch`, …), the `initializeOrder`, and the
*Add Sub Context* button that lists every `Context` type in the project.

---

## Related

- [README — FlowIoC at a Glance](../README.md#flowioc-at-a-glance)
- [Code Generator](CodeGenerator/Documentation.md) — the generators in detail
- [Flow Console](../Runtime/ConsoleModule/Documentation/FlowConsole.md)
