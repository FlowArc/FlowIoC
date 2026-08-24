# Code Generator

FlowIoC modules have a fixed shape: a folder tree, an assembly definition, a
`_module_info.txt`, a namespace derived from the path, and a `Root` / `Context` pair.
The generators create all of that correctly and keep it correct when things move.

The rule of thumb: **never create a module, view or screen by copying an existing
one.** A copy carries the source module's namespace, its asmdef name and its module
info, and every tool downstream — the diagram, the sub-context picker, the console's
log channels — keeps finding the original.

- [Create Module](#create-module)
- [Create View](#create-view)
- [Create Command](#create-command)
- [Create Model](#create-model)
- [Namespaces and Module Info](#namespaces-and-module-info)
- [Delete Module](#delete-module)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## Create Module

**Tools ▸ FlowIoC ▸ Create Module**

Name the module, pick a type, and choose which optional folders you want.

### Module types

| Type | For | Produces |
|---|---|---|
| `Main` | A feature or service — gameplay, economy, audio | `Assets/Modules/<Name>Module/` with the standard tree, `Modules.<Name>.asmdef`, `<Name>Root` and `<Name>Context` |
| `Screen` | One UI screen | The above plus a `ScreenView` / `ScreenMediator` pair, the screen config, and optionally a scene |
| `Test` | An isolated test harness for another module | The above, wrapped in editor-only preprocessor directives so it never ships |

### Options

| Option | Effect |
|---|---|
| `Create Root` / `Create Context` | Generate the pair. Leave both on unless you are adding a module that will only ever be a sub-context. |
| `Create Scene` | Add a scene, so the module can be opened and played on its own |
| `Make Root Singleton` | Derive the Root from `SingletonRoot<TContext>` instead of `Root<TContext>`, so it survives scene loads and refuses duplicates. For app-wide modules — audio, analytics, player profile. |
| Actions (Screen only) | Names such as `OnBackButtonClicked`. Each becomes a callback on the View and a handler on the Mediator. |
| Optional folders | `Resources`, `Editor`, `Scenes`, `Prefabs` and the rest — add what the module needs |

### What lands on disk

```
Assets/Modules/EconomyModule/
├── Modules.Economy.asmdef
├── _module_info.txt
└── Scripts/Runtime/RootsContexts/
    ├── EconomyRoot.cs
    └── EconomyContext.cs
```

Plus whichever optional folders you selected. `_module_info.txt` is what the other
tools read to know this folder is a module and what kind — do not delete it.

Naming a Screen module: give it the screen's name (`Settings`, `DailyReward`), not
`SettingsScreenScreen`. The generator adds the suffix.

---

## Create View

**Tools ▸ FlowIoC ▸ Create View**

Pick the module and sub-module, name the view, and the generator writes three things:
the `View` class, the `Mediator` class, and the prefab with a `ViewInjector` component
already on it.

That third item is the reason to use the tool rather than writing the pair by hand. A
view without `ViewInjector` compiles, binds, and then silently never registers — the
mediator's `OnRegister` never runs and nothing in the console says why.

The generated mediator comes with empty `OnRegister` / `OnRemove` bodies. Whatever you
subscribe in the first, unsubscribe in the second; mediators are pooled, and a
leftover subscription comes back attached to the next view.

Remember the binding — the generator writes the classes, not the line in your context:

```csharp
public override void MediationBindings()
{
    base.MediationBindings();
    MediationBinder.Bind<InventoryView>().To<InventoryMediator>();
}
```

---

## Create Command

**Tools ▸ FlowIoC ▸ Create Command**

Writes a command into the module's `Controllers` folder with the right namespace. It
does not write the binding — add that yourself in `CommandBindings()`:

```csharp
CommandBinder.Bind(_signals.Incoming.Purchase)
    .ToSequence<ValidatePurchaseCommand>()
    .ToSequence<GrantItemCommand>();
```

Name commands after what they do to the world — `GrantItemCommand`,
`PersistMatchResultCommand` — not after the signal that triggers them. A command named
`OnPurchaseCommand` cannot be reused in a second chain without lying about itself.

---

## Create Model

**Tools ▸ FlowIoC ▸ Create Model**

Writes the `IXModel` / `XModel` pair into the module's `Models` folder. Bind it
yourself:

```csharp
InjectionBinder.Bind<IEconomyModel, EconomyModel>();                 // module-private
InjectionBinderCrossContext.Bind<IEconomyModel, EconomyModel>();     // shared
```

Default to the module-private binder. Cross-context is for models other modules
genuinely read, and every one of those is a coupling you will have to maintain.

---

## Namespaces and Module Info

Namespaces are derived from the module's path, so moving a folder in the Project
window desynchronises the code from its location. Two tools fix it:

**`Assets ▸ FlowIoC ▸ Update Module's Namespaces`** — select the module folder and
every namespace inside is rewritten to match where it now lives. Run it immediately
after a move or a rename.

**`Tools ▸ FlowIoC ▸ Module Configuration`**

| Item | Does |
|---|---|
| `Detect & Fix Module Infos` | Finds modules whose `_module_info.txt` is missing, stale or pointing at an old path, and repairs them |
| `Cleanse Module Infos` | Removes metadata left behind by modules that no longer exist |
| `Update Namespace Settings` | Changes the namespace prefix the generators use for new code |

The symptom that you needed *Detect & Fix* is a generator writing into the wrong
folder, or a Root whose sub-context list has gone empty after a move.

---

## Delete Module

**Tools ▸ FlowIoC ▸ Delete Module**

Removes the folder, its assembly definition and its module metadata together.

Deleting by hand leaves the asmdef reference in every other module that referenced it,
and Unity reports that as a compile error naming the *referencing* module — not the
one you deleted.

Before deleting, search the project for references to the module's signals and
models. Connector wiring is resolved at runtime, so a
connector still pointing at a deleted module's signals fails when the scene runs, not
when the project compiles.

---

## Scenarios

### Generate, do not copy

```
✅ Tools ▸ FlowIoC ▸ Create Module → "Inventory", type Main.
   Correct namespace, correct asmdef name, correct module info.
```

```
❌ Duplicate EconomyModule, rename the folder to InventoryModule, find-and-replace
   the namespaces. The asmdef is still named Modules.Economy, _module_info.txt still
   describes Economy, and the sub-context picker still lists the wrong contexts.
```

### Fix namespaces at the moment you move something

```
✅ Move the folder, then run Update Module's Namespaces, then compile.
```

```
❌ Move the folder and keep working. The next generated class lands in the old
   namespace, and the mismatch surfaces days later as a missing sub-context.
```

### Let the tool make the prefab

```
✅ Create View writes the view, the mediator and a prefab that already has
   ViewInjector on it.
```

```
❌ Hand-written view and prefab. It compiles, the mediation binding is there, and
   OnRegister never runs — with no error to search for.
```

### Name a command after its effect

```csharp
// ✅ Reusable in any chain that needs this to happen.
public class GrantItemCommand : Command { }
```

```csharp
// ❌ Named after its trigger. The second chain that needs it has to either rename it
//    or live with a lie.
public class OnPurchaseSignalCommand : Command { }
```

---

## Pitfalls

### The generator writes into the wrong module

`_module_info.txt` is stale — usually after a folder move. Run *Module Configuration ▸
Detect & Fix Module Infos*.

### A new module does not appear in the sub-context picker

The picker lists `Context` types found in the assemblies. Check that the module's
asmdef exists and that the project compiled; a module whose assembly failed to build
contributes no types. Also check for `[ExcludeFromContextWindow]`, which hides a
context deliberately.

### A generated Test module ships in the build

Test modules are wrapped in editor-only preprocessor directives, but only the files
the generator wrote. Anything you add afterwards needs the same guard, and the test
module's asmdef should stay out of the build's assembly graph.

### Two modules end up with the same assembly name

Copying a module and renaming only the folder. Assembly names must be unique;
Unity's error names the collision but not the copy that caused it. Delete the copy and
generate the module properly.

---

## Related

- [Editor Tools](../README.md) — everything else in the FlowIoC menu
- [README — Module Layout Convention](../../README.md#module-layout-convention) — the
  folder shape the generator produces
- [Base Module](../../Runtime/BaseModule/Documentation/BaseModule.md) — what goes in
  the generated Root and Context
