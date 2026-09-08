# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.10.0] - 2026-09-08

### Added

- **Module Scanner reports a module whose log type part is missing.** `LogTypeCheck` answers
  whether the channel is registered in `CD_FlowConsole`; nothing was watching the other half, the
  generated part in the module and the asmref beside it. Either can go without the settings
  changing at all - deleted by hand, lost in a merge, never written because the generator was
  interrupted - and the first anyone hears of it is the module failing to compile against a
  constant no longer declared anywhere. The missing asmref is reported on its own, because without
  it the part still compiles: into the module's own assembly, where it is a second `FlowLogType`
  sharing a name with the real one and nothing else. Both repair by running the generator, so there
  is one writer and one shape.

### Changed

- **A log channel is a name, not a number, and a module declares its own.** `FlowLogType` used to be
  one generated file listing every channel in the project as a `const int`, and the numbers were
  handed out in order and reassigned whenever the list was sorted - so a module whose name sorted
  early moved every channel after it onto a different number, taking every saved filter and every row
  already recorded with it. There was also nobody to hand out the next number: two people adding a
  module on two branches were each handed the same one, and they conflicted over the same lines of
  the same file. Both problems are gone. A channel is identified by its name, and each module's
  channel is a `const string` in a part of its own at
  `<Module>/Scripts/Generated/FlowLogType.<Module>.cs`, with a `FlowIoC.Generated.asmref` beside it -
  parts of a partial class must share an assembly, and each module has one of its own, so the asmref
  is what makes the part possible. The shared file keeps only what belongs to no module: `Default`,
  and any channel added by hand.

  What this changes for a caller: `FlowLogger.Log`, `LogWarning`, `LogError` and `LogLong` take a
  `string` channel where they took an `int`. Every call written as `FlowLogType.PlayerModule` goes on
  compiling. `ConsoleLog.LogTypeValue` became `ConsoleLog.Channel`, saved filter presets store names,
  and `GetModuleLogType<T>()` answers with a name or null rather than `-1`.

- **A signal says which holder and which half it came from.** A signal is constructed knowing only
  its own field name, which the compiler hands it through `[CallerMemberName]`, so the console said
  `'Launch' dispatched` and left the reader to guess whose Launch that was, out of which holder.
  The name is now qualified - `CameraSignals.Incoming.SwitchCamera`,
  `CounterServiceInternalSignals.Tick` - and it costs nothing to dispatch: the walk that marks each
  signal as the framework's or the game's already has the owner chain in hand, so the name is
  composed there, once, when the holder is bound. A signal that is not a field of a stamped holder
  keeps the short name it was constructed with. The `CommandBinder` lines and the "bound twice"
  error read the same name and say more for it.
- **The Filters panel lists the project's modules first**, then the framework, then Unity - the
  order of how much a reader cares. The group ids behind the panel are unchanged, because they key
  the mute snapshot and the saved panel state.

### Fixed

- **A context renamed inside its file reaches the Roots that list it.** A sub-context entry holds two
  things about one context: the script it is declared in, and its full name, which is the only half
  runtime reads. Renaming the class carried the script reference along with the file and left the
  name behind - so the entry still read as linked, nothing was drawn under it, and the Root quietly
  built nothing for a name that resolved to nothing. `SubContextNameSync` could already say whether
  an entry had drifted and was called from nothing but its own tests; the Root inspector now runs
  that pass before it draws the list. An entry that has not moved is not written, so a repaint
  neither records an Undo step nor dirties the Root.

- **A deleted module takes its sub-modules' assemblies with it.** Delete Module worked out which
  assemblies belonged to a module from the module's name - the module's own, plus `.Shared` and
  `.Signals` - and a module that holds sub-modules declares more than three. Deleting a screen module
  left `Modules.<Name>.Screen.Test` named in every asmdef that referenced it, and its `.csproj` and
  `.csproj.DotSettings` at the solution root pointing at an assembly that had gone; a main module
  holding several screen modules left all of theirs. `ModuleAssemblies` reads the names out of the
  asmdefs inside the folder instead, and both the reference unwiring and the project-file removal ask
  it. The three the name implies are kept alongside what was found, so a module whose folder has
  already been half removed still has its own assemblies unwired.

- **An entry's warning wraps instead of running off the Inspector's edge.** What those lines name is
  a namespaced type, and at a docked Inspector's width `Nothing compiles to <full name>. Its module
  is gone or renamed.` ended mid-name - the half that says what happened was the half that went. The
  line wraps now, and the unresolved one, having no button beside it, is drawn on its own and takes
  the entry's whole width.

- **Create Module keeps its button and its action list in view.** The form now scrolls, and the
  module card and the Create Module button sit below it rather than inside it, so a screen module's
  settings and its action list can no longer push the button past the window's floor. The parent and
  folder-preview panels are drawn shorter on a screen module, and the action list takes whatever
  height is left above the card, so widening the window lengthens the list instead of leaving it at a
  fixed 60 pixels.

## [1.9.1] - 2026-09-08

### Changed

- **A command group drives its steps from a loop rather than by recursion.** `CheckExecuteNextStep`,
  `ExecuteCommandStep` and `HandleStepCompletion` formed a closed cycle, so a step did not return
  before the next one started and a whole sequence's frames stayed on the stack until it had
  finished. The group could therefore end and the resolver go back to the pool while frames of that
  same run were still waiting to resume: a command that dispatched a signal took the instance out
  again, `Initialize` reset the run, and those frames woke up on somebody else's steps. A single
  `Pump()` now starts every step from one frame - a completion that lands while it is running says
  so and returns - and the group is ended by the driver's exit, when nothing of the run is on the
  stack. The two run-id guards that caught this after the fact are gone; the one in the sub-group
  callback stays, because a retained sub group can still report after its parent was pooled and
  re-initialised, and `RunToken` stays because that one belongs to the command pool. No public type
  or signature changed.

- **A sub group is told who to report to instead of being subscribed to.** Starting a group step
  built a closure and hung it on the sub resolver's `GroupExecutionFinished`, which cost two
  allocations every time - the delegate, and the display class that capturing the parent's run id
  forced - and the shipped `CounterModule` runs one such step every second for as long as its
  service is alive, because its tick re-enters itself through `ToGroupAsParallel`. The sub resolver
  is pooled, so it carries the parent and that run id in two fields of its own now and reports
  directly. The binder's own path was already allocation free with a cached delegate; this was the
  one place that had not been given the same treatment. The run-id check did not go anywhere - it is
  passed rather than captured - and a dispatch's own group is still finished through the listener
  the binder subscribes, the two being exclusive. No public type or signature changed.

- **A log line names the class and the method it came from.** `1.9.0` moved the framework's own
  messages to prose that read subject-first, and what went with it was the one thing the old lines
  were good at: saying where in the code they were written. The shape is now three parts - the
  channel's tag from its profile, then `[Class.Method]`, then one bracket per variable:
  `[Screen] [ScreenService.Hide.ScreensAtManager][manager(2)][force(True)]`. A sub service a caller
  reaches through its parent is written as that call path, because the path is what the reader
  typed; one that is private inside its owner is written as its class name. A message with no
  variables ends at the bracket, with no verb after it. Forty-eight lines across Asset, Command,
  Pool, Screen and Injection took the new shape.

  The engine's own lines are the exception and stay subject-first, because the bracket would name a
  framework class the reader has no use for: a signal dispatching, a command executing, the function
  provider, and the Context, Root and RootsManager lines. Warnings and errors keep the bracketed
  name of the guard that is speaking, which they already had.

### Fixed

- **The console colours a new project is given are the ones the console was tuned to.** A channel's
  colour is read from `CD_FlowConsole`, and that asset belongs to the project rather than to the
  package - so the colours picked while `1.9.0` was being finished lived only in the workspace they
  were picked in, and a project installing the package got the older palette. `Signal` and `Asset`
  had no entry at all and came out white. The defaults in code now carry what was chosen, and a
  channel's profile takes its tag colour from the channel, so the tag follows.
- **`Models`, `Services` and `Editor` are typed as themselves in a main module's layout.** The
  folder list is declared twice - as the field initializer and again in
  `InitializeDefaultFolderStructure`, which is the one a new project's asset is written from - and
  the two had drifted: the initializer typed those three as a plain `Folder`. Nothing in a project
  read it, because the method is what runs, but anything that creates a layout without initializing
  it saw folders that `FindFullFolderPathByID` cannot find, since that lookup matches on
  `FolderType` and nothing else. The screen layout's `Constants` had drifted the other way, mandatory
  in the method and optional in the initializer, and is optional in all three layouts now. The test
  that compares the two declarations missed both: it keyed folders by bare name, so the `Constants`
  under `Shared` overwrote the one under `Runtime`, and it never compared the type at all. It keys
  by path and compares the type now.
- **A retained command is remembered against its step rather than against an index into a list.**
  `StopCommand` read the step back out of `_steps` by index and returned when that index was out of
  range - having already handed the command to the pool, and without reporting - which left the
  group waiting on a step nobody would ever finish. The dictionary now carries the step itself, so
  the lookup that could be answered wrongly is gone rather than guarded.

## [1.9.0] - 2026-09-08

### Added

- **A row in Flow Console opens the code it is about.** A line that names a type carries the type
  and captures no stack: a command executing opens the Command, a function running opens the
  Function, a screen opening opens its View. The stack at that moment answers a different question -
  a command's execute line is written while the Context that dispatched the signal is still below
  it - and the type is the better answer for nothing. A signal the game dispatched from inside a
  sequence points at the `Bind` line that declared the sequence, taken from the compiler at the call
  and so free; one dispatched from a Mediator or a `Launch` still looks at the stack.
- **A Connector says so.** A crossing had no line of its own - the target simply dispatched, and
  nothing named the Connector or where to read the wiring. It now writes `[Connector] 'A' to 'B'`
  and both that row and the dispatch it causes open the `Connect` call, whose file and line the
  compiler wrote into it.
- **The framework's own signals moved off the Signal channel**, onto `SignalOperation`, the way
  `Command` and `CommandOperation` are already split - so `Signal` is the game's traffic and every
  row on it is one the reader wrote and can open. Which a signal is comes from its holder's
  assembly, read once when the holder is bound. Commands bound by the framework's own Contexts
  moved to `CommandOperation` for the same reason, and the plumbing channels work out no source at
  all: there is nowhere to take a reader that would tell them anything.
- **A profile per channel, written from code**, so a project that has just installed the package has
  them. The profile is where a channel's tag lives - `[Signal]`, `[Command]` - and it takes the
  channel's own colour, so recolouring a channel recolours its tag. They are mandatory and not
  editable: a tag somebody renamed no longer matches what the documentation says the console prints.
- **The console's own layout.** The channels moved out of a strip across the top into a panel down
  the right behind a `Filters` switch, grouped into Unity's output, the framework's channels and the
  project's modules - each foldable, each saying how many of it are showing, each with a `Mute` that
  silences the group without switching anything off. A row is the switch rather than carrying one,
  and alt+clicking one isolates that channel until you leave isolation. `Settings` puts the two
  dials a reader turns while following a flow - how many lines a row shows, and how much a log works
  out about where it came from - along the bar beside `Export`. The severity counters became icons
  in the toolbar the way Unity draws them, and `Focus Log` appears only while the selected row is
  off screen.

- **Flow Console is a console you can keep open instead of Unity's.** Unity's own output arrives on
  two new channels, `Unity` and `Compiler`, read through `Application.logMessageReceived` and
  `CompilationPipeline` and nothing else, so a Unity upgrade cannot quietly break them. Both are
  recorded whether or not `ENABLE_LOG` is defined - turning the define off is a statement about your
  logging, and a console that then showed no compile errors would be useless at the moment it is
  most needed. With them come the switches Unity's console has: a `Clear` split button carrying
  Clear on Play, Clear on Recompile and Clear on Build; `Collapse`, which folds equal rows onto one
  line with a count and keeps the place of the first occurrence; `Error Pause`; severity icons; rows
  of one, two or three lines; a list that follows new logs down while it is resting at the bottom
  and leaves you alone once you scroll up; and arrows, PageUp/PageDown, Home/End, Enter and Ctrl+C
  on the list.
- **The log list survives a recompile.** `FlowLogger.Logs` is a static, so a domain reload used to
  empty the console at the moment a compile error most wants reading, whatever Clear on Recompile
  said. The newest five thousand rows are now written to `SessionState` before the reload and read
  back after it.
- **The search box parses what was typed.** Terms are ANDed, a `-term` excludes, and a term wrapped
  in slashes is a regular expression; a half-typed expression says so rather than quietly emptying
  the list. What matched is painted behind the text, because a narrowed list says which rows
  survived but not why, and beside the severity counters is how many rows are shown against how many
  the console holds.
- **Alt+clicking a channel narrows the console to it**, and alt+clicking the one that is already
  alone brings the rest back. `Presets` keeps a set of channels under a name - two ship with the
  console, the rest are saved from what is shown - in EditorPrefs, because `CD_FlowConsole` is
  committed and one developer's filter has no business in everybody's diff.
- **`Flow` groups the rows into the flows they belong to.** Every log written while a signal runs
  its commands now carries the id of that flow, and one started from inside another carries its
  parent's, so each flow opens with a line that folds it away and a nested flow sits indented under
  it. Read straight down, a busy frame is four operations interleaved. The ids cost a shipping build
  nothing: the calls that keep them carry `[Conditional("ENABLE_LOG")]`.
- **Pinning, timing and session lines.** Pinning a row - `P`, or its right-click menu - is you
  saying this one is not noise, so it outranks the channel switches, the severity toggles, the trim
  at `MaxLogCount` and the automatic clears; only the `Clear` button and a search go past it.
  `Timing` swaps a row's clock for the frame it was written in and the gap since the row above. A
  line is drawn across the list wherever a play session begins or ends.
- **`Export`** saves or copies the rows that are showing as plain text, filters and search included,
  because the rows you narrowed down to are the ones worth sending.
- **Create Function**, beside Create Command in `Tools/FlowIoC`. It writes one file into the
  module's `Controllers` folder and touches no Context, because a function is called from inside a
  Command rather than dispatched. What the window is for is the base type: the kind, the parameters
  and the return type decide which of the shipped arities the class derives from, and the three have
  to agree - `FunctionReturn<double, string>` with a `public override double Execute(string)` and
  the right `using` - which is where a hand-written function goes wrong. An async function's type
  argument is the value its callback carries rather than a parameter, so the parameter rows are not
  offered for one at all.

  It also shows **how the function is called**, in the Help window's own code block with the same
  colouring and the same Copy button. That is the half a generator cannot write: a function says
  nothing about where it is called from, which is the whole difference between one and a Command.
  The snippet follows the fields as they are filled in - the kind picks the terminator, the
  parameters become the arguments under the names that were typed, and a callback type turns the
  call into the `CallAsync<T, TValue>` form with a callback to hand in.

### Changed

- **The framework's own log lines have one shape: the subject, then what happened.** `'OpenMainScreen'
  dispatched (0 parameters)`, `OpenMainScreenCommand executed as Sequence`, `MainScreenView opened`,
  `MainContext | setup`. The channel's name is not repeated in the text - the channel is the column
  the row is in, and the profile puts the tag on the front - and the subject leads because that is
  what a reader scans the column for. The bracketed names stay in warnings and errors, where they
  are not a channel tag but the guard that noticed speaking.
- **`ICommandBinder.Bind` takes the caller's file and line**, as optional arguments the compiler
  fills in. Source-compatible with every call already written; a binder implemented outside the
  package needs the two parameters added to its signature.
- **A Function lives in `Controllers/` with the Commands, and the `Functions/` folder is gone.** A
  Command and a Function are the same kind of thing - neither holds state, and both do the module's
  work - so a module has one folder for its controllers rather than two. Create Module no longer
  writes `Functions/`, and a config asset written while it did has the entry healed out the next
  time the Editor opens; the folder itself is left where it is, along with anything in it, because a
  heal that runs on every open is no place to move a developer's code.
- **A function derives from one of the shipped arities, and the compiler is what says so.**
  `FunctionBody`'s constructor is internal and its `TryInvokeExecute` is abstract, so a class can no
  longer be written straight on it. That class used to compile and run: the provider found its
  `Execute` by name and called it through `MethodInfo.Invoke`, at a boxed call per run, and nothing
  reported that the function had been given the wrong base. The fallback and its `MethodInfo` cache
  are gone with it, and every function is now called through its own typed entry. An `AsyncFunction`
  reached with `Call` instead of `CallAsync` is reported by name rather than doing nothing.

### Fixed

- **A compile error was two rows.** Unity echoes every compiler message through
  `Application.logMessageReceived` as well, so the console held the `Compiler` row that knows the
  file and line and, beside it, Unity's copy that knows neither. The bridge recognises the echo by
  its shape - `(line,column): error CSxxxx:` - and drops it, so the row that stays is the one that
  opens the code.
- **A row whose whole stack is the framework is clickable again.** The frame filter skips the
  package's own frames to find the game's, and a log the framework writes about itself has none - so
  the row got no location at all. Failing to find a game frame now falls back to the first frame that
  has a file and a line.
- **An injectable a generator could not place is written anyway rather than dropped.** Create
  Command, Create Function and Create Model looked each injected type up by name and, finding
  nothing, left the member out of the file - silently. A name with a typo in it, or a type in an
  assembly the module does not reference, simply was not there, and the author was left to work out
  which of the fields they had filled in was the reason. Every name given is written now: one whose
  type the project has brings its using with it, and one it does not have is written regardless,
  because a member that does not compile is a compiler error naming the file, the line and the
  column - and nothing at all is not a report. FlowIoC adds no message of its own beside it, which
  would say the same thing less precisely. Create Function does say so before the file is written,
  though: a row naming a type the project does not have wears a warning, so the mistake can be
  fixed rather than compiled. The lookup became an index built once rather than a walk over every
  loaded assembly per question, which is what makes asking on every repaint affordable.
- **A generator selects and pings the file it just wrote.** Create Command, Create Function, Create
  Model and Create View write into a folder the author may not have open, and a file that cannot be
  found reads as a generator that did nothing.
- **The four single-file generators can no longer be filled in and then not used.** Create Command,
  Create Function, Create Model and Create View each hold a list that grows a row at a time -
  injectables, a view's actions, a function's parameters - and laid their sections out one under the
  other with no scroll of their own. Enough rows pushed the module list and the window's own button
  past the bottom with nothing to scroll them back. The body scrolls now and the button does not: it
  is drawn below the scroll view and stays where it is however long the list gets. The three inner
  scroll views went with the fix, so a window has one scrollbar rather than one per list - a list
  with its own scroll inside a scrolling page traps the wheel over whichever one the pointer is on.
- **Leaving play mode no longer throws a `MissingReferenceException` for every open screen.** The
  Root's `OnDestroy` dispatches `UnRegisterScreen`, and by then Unity has already destroyed the
  screen - but an `IScreenBody` is an interface, so the `== null` guards on that path compared the
  managed reference and let the dead instance through to the passive pool, which reads its
  transform. `IView.IsAlive()` is what those guards ask now: an unregister that finds its screen
  destroyed drops it from the pools and releases the loader's handle without hiding it, and the
  two loaders skip the `Destroy` they can no longer do.
- **A diagnostic opens the code that caused it, not the guard that noticed.** Double-clicking a
  warning about a command that released without retaining used to open `CommandGroupResolver` - the
  framework checking a condition - which tells the reader nothing they did not already know. The
  console now skips the framework's own stack frames when it works out where a log came from, and
  the diagnostics that know which type they are about say so, so an asynchronous release still
  opens the Command.
- **A log's source can no longer take the console window down.** A path read out of a stack trace
  is whatever text was there, and a generated frame carries angle brackets that `System.IO.Path`
  refuses with an `ArgumentException`. Thrown inside `OnGUI` it unbalanced GUILayout and broke the
  window's drawing once per repaint.
- **The log list is no longer wiped at the start of every run.** `ResetStatics` cleared it as play
  mode began, after the reload had just carried it across, so Clear on Play being off changed
  nothing and a pinned row went with the rest. What the console holds when a run starts is Clear on
  Play's decision alone. The trim at `MaxLogCount` counts pinned rows as kept rather than dropping
  the oldest of them, and the runtime trim and the window's now go through one trimmer.

## [1.8.0] - 2026-09-07

### Fixed

- **A function that releases itself inside its own `Execute` is no longer pooled twice.** The
  provider pools a function whose run left nothing retained, and `Release()` cleared `HasRetain` on
  the way out - so a function that retained and released mid-`Execute` was pooled by its own
  `Release` and then again by the check after it, and two callers were handed one instance. Both
  halves of the command engine's answer are here now: `FunctionBody.Dispose` leaves `HasRetain`
  alone and a `BeginRun` clears it when the instance is taken out, and a `RunToken` says whether the
  run that took an instance out is still the run holding it - for the case where a nested call of
  the same type took it out again mid-`Execute`. A Function may now retain and release inside one
  `Execute`, the way a Command always could.

### Changed

- **A function is called with `Call<T>()` and run with `Execute()`, `ExecuteAsync()` or
  `ExecuteAndGetResult<T>()`.** Both halves of the chain said the wrong thing.
  `IFunctionProvider.Execute<T>()` did not execute - it names the function and hands back a chain,
  and nothing happens until the chain is ended - while the terminators `SetVoid()`, `SetAsync()` and
  `SetReturn<T>()` said assignment about the lines that actually run it. The provider says `Call<T>()`
  and `CallAsync<T>()` now, and the terminator says `Execute`, which is the same word the function's
  own method carries and the same word a Command runs under. The three terminators share the
  `Execute` prefix on purpose: typing `E` after the dot offers all three, rather than making a caller
  know in advance which one this function needs. `ExecuteAndGetResult<T>` is longer than a bare verb
  for the reason `SetReturn` was named that way in the first place - the type parameter is what comes
  back, and a shorter `Execute<double>()` would read as a parameter going in.

### Fixed

- **A screen test scene clears its authored screen in `Awake`, and no longer calls a Unity message
  by hand.** The scene keeps the screen's prefab in it so it can be edited there, and the run opens
  the screen from code instead - so the authored instance has to go before anything registers it.
  `BaseScreenTestRoot` was clearing it in `AfterCreateBeforeStartContext`, which is the middle of
  Unity's Start phase: the screen's own `ViewInjector` may have started first, found no context
  started yet, and subscribed to `OnContextReady` - which `StartContext` raises a few lines later.
  The instance on its way out then had its Mediator built and registered, and undoing that is why
  the Root called `ViewInjector.OnDestroy` itself. Clearing in `BeforeCreateContext` is before every
  `Start`, so there is nothing to undo and the hand-written call is gone. The screen is deactivated
  before it is destroyed, because `Destroy` is deferred to the end of the frame and deactivating
  says now, without depending on when the frame ends.

### Changed

- **The naming and API nits a review had left alone, now that there is nothing to break.** Each one
  touches a public surface, which is why they waited; they are cheap today and dearer later.
  `IConstructable.IsDeConstructed` is `IsDeconstructed`. `ICoroutineProvider` and `IUpdateProvider`
  take `Action` rather than `UnityAction`, which was never a Unity event to begin with. `IContext`,
  `ICommandBody`, `IFunctionBody` and `IScreenService` declare a setter only where something outside
  the implementation actually writes one - `ICommandBody.IsRetain` and `HasRetain` were writable from
  outside the engine, which is the flag the resolver decides a step by. `SignalConnector`'s
  `"signalName"` sentinel is a plain `null`, its `Disconnect` takes the `ISignalBody` its `Connect`
  takes rather than the concrete `SignalBody`, and the `[ShowInModelViewer]` on its static field is
  gone - the Model Viewer reflects instance members only, so it never did anything.
  `PoolServiceSignals` is `PoolServiceInternalSignals` and `internal`, next to
  `ScreenServiceInternalSignals`: it has no `Incoming` and no `Outgoing` because its two signals are
  traffic between the config adapter and the service, and an internal holder is what that is.
  `InjectionBinder`'s two `BindInstance` bodies and two `CreateInstance` bodies are one each, since
  they differed only in which type the instance is filed under.

- **A context fills what its binder made, and nothing else.** `InjectAllInstances` walked the shared
  binder too, and an instance handed in with `BindInstance` records no context - so it was injected
  once per context and the last Root in Initialize Order decided what its `[Inject]` members
  resolved to. Nothing bound that way has any today, which is the only reason it was never seen.
  Whoever hands an object in fills it, the same way it belongs to them at teardown.

- **A log message spells names out; `nameof` is no longer used in one.**
  `$"{nameof(Execute)} - {nameof(AddCurrencyCommand)}"` written inside `AddCurrencyCommand` is a real
  reference to the type, so Find Usages and a plain search answer *where is this Command used* with
  the command's own logging lines - and the answer wanted, which Context binds it and which signal
  runs it, is buried under hits from the file the reader is already in. Every log in the package, in
  the modules it ships and in its documentation now carries the name as a literal, including the
  places that fed one in indirectly: `CommandBody`'s detached-call report, `LoadSubService`'s caller
  label and `LocalSaveService`'s prefix. The trade is that a rename leaves the literal stale, and a
  stale word in a log line is cheaper than a search that cannot be trusted. `nameof` is untouched
  everywhere else - `[Inject(nameof(PlayerContext))]`, a `Group` constant, an `ArgumentNullException`
  parameter name. The rule is in `AgentRules.md`, the README, `FlowConsole.md` and the Help window's
  Flow Console page.

- **The rule about resolving a retain now covers an `await`.** "Every path out of a retained Command
  ends in `Release()` or `Stop()`" was already written down, and the shipped example of an
  asynchronous command did not obey it: it awaited, and only the success path resolved the retain.
  An `await` has three ways out - the work returned, it came back with nothing, and it threw - and
  the two nobody writes are the ones that hang the group for ever, silently, because a retain has no
  timeout. A throw is the worse of the pair: it leaves `Execute` at the `await` line and, being
  `async void`, surfaces through Unity's unhandled-exception handler with nothing in it to name the
  command. `OpenMainScreenCommand` and `OpenGameplayScreenCommand` wrap their work in `try`/`catch`
  and answer all three, and use `Show<T>()` rather than `Show()` so the null check runs through
  Unity's own operator instead of an interface reference comparison. The rule is in `AgentRules.md`,
  the README, `Controller.md`, the Help window's Controllers page and the `flowioc-screens` skill.

  What the `catch` does stays the game's decision - `Stop()`, a `Release()` that carries on, a signal
  that opens something else - which is why no base class writes it. An `AsyncCommand` that retained
  and released around `ExecuteAsync` was built for this and reverted the same day for taking that
  decision into the package.

- **`AssetModule`'s `LoadGroupCommand` follows the same rule.** It released only when the load
  finished, so an exception in it hung the group. It also let an empty label through: the service
  answers one with a finished task, no group and no `GroupLoaded`, so the steps behind it carried on
  believing the group was in memory. The command names that case before it starts and stops, stops
  on a throw, and releases when the group is actually loaded.

### Added

- **`RemoveAllListeners()` on every signal.** A Mediator's `OnRemove` was the only teardown path a
  listener had, so a signal that outlived the objects listening to it kept every listener a
  destroyed one left behind. The call is declared on `ISignalBody`, so it reaches a signal of any
  arity without the caller knowing which, and it drops the once-listeners with the ordinary ones.
  What a dispatch runs in the middle - the commands bound to the signal - is left alone, because
  that belongs to the context that bound it and goes when the context does. `SignalBody` is
  `abstract` now, which it always was in practice: it is never instantiated, only inherited by the
  five arities.

- **The tests the engine was missing.** `StopCommand` on a parallel step, which ends that step alone
  while a sequence step's Stop ends the group; a Release or a Stop arriving after its group already
  ended, which must change nothing and above all must not pool a busy command twice; the injection
  binder's assignable-type cache, whose remembered answer - a miss included - has to be forgotten
  when the container changes, and the binding generation that makes a pooled command resolve again;
  `ShowSubService` and `HideSubService` against a runtime model that only records what it was asked;
  and a scene reload driven through real Roots rather than bare contexts, where a Connector rebuilt
  with its scene has to be wiring the holders the rebuilt modules bound. 1119 tests, up from 1084.

### Changed

- **`Signal.Dispatch`'s order is written down.** Once-listeners, then the commands bound to the
  signal, then the ordinary listeners - so `AddListener` is the one a Mediator uses to redraw from
  a value a command has just written, and `AddListenerOnce` sees the dispatch before that command
  ran. It is on the Signals help page, in `BaseModule.md` and in the README, and a test asserts the
  order rather than leaving it to be rediscovered.
- **A screen already being loaded is waited for rather than refused.** `ScreenModule`'s
  `AddressableLoadSubService` answered a second load of an address still in flight with a warning
  and a null screen. Nobody asked for that: the caller meant the load, and the same address is
  loaded twice legitimately when one screen is registered at two managers. It now awaits the handle
  already loading, the way the pool's loader does, and returns the entry's screen if that load
  filled it rather than instantiating a second copy of it.
- **A missing command group names the context it was looked for in.** `ExecuteGroupStep` reported
  that the group "could not be found in any context" while looking in the binding context alone,
  which sent the reader off to check every other Root in the scene. It says which context, and that
  a group is looked up in the context that binds it and nowhere else.

### Removed

- **The screen builder's `AddToHistory()` is gone.** It set `ScreenVO.AddToHistory`, which nothing
  read: `ShowSubService` carried a `//TODO: History` where the history would have been written, so
  the call promised a navigation history the module never kept. The flag, the method on
  `IScreenBuilder` and `ScreenBuilder`, the reset in `ScreenRegistryModel.CopyDataFromConfig` and
  the documentation row went with it. A screen history, if one is wanted later, is a module of its
  own rather than a flag on the open call.

## [1.7.3] - 2026-09-07

### Added

- **The Help window colours its code blocks.** Snippets are lexed and drawn in Rider's own palette -
  Rider Dark under the dark skin, Rider Light under the light one - so a snippet in the window and
  the same lines open in the IDE read alike. The block paints its own fill and hairline and carries
  a Copy button, because a rich text label cannot be selected the way the plain one it replaces
  could.

### Changed

- **A context takes back what it bound across.** `DestroyContext` emptied the context's own binder
  and left the shared one alone, so a module's signal holder and Service outlived the module. When
  its Root was built again - a scene coming back - `Bind` handed the old instances back, and the
  old Service was still pointing at the sub services and models the old context had already torn
  down: `InjectAllInstances` tried to resolve it against that dead context and logged an injection
  failure per member, twenty-four of them for the pool service alone, and the screens and pools of
  the new scene never reached it. A context now takes out of `InjectionBinderCrossContext`
  everything it bound there, after its own binder and before it goes, so a module's public surface
  lives exactly as long as its Root: a scene's module goes with the scene and is bound fresh when
  the scene comes back, a persistent Root's Service lives for the run, and what was handed in with
  `BindInstance` - the two providers - stays. The README, the agent rules and the Help window state
  the rule together with its corollary: a Connector, the one thing that holds another module's
  signals, gets them in `Setup` and disconnects them in `DestroyContext`.

## [1.7.2] - 2026-09-07

### Added

- **`FlowLogger.IsEnabled`, and log overloads that take the message in parts.** `[Conditional]`
  removes a log call only where `ENABLE_LOG` is not defined; a project that defines it - this one
  does, for Standalone, Android and WebGL - still built every interpolated message with logging
  switched off. The parts overloads join them only when logging is on, and `IsEnabled` is for a
  call site that has to build something before it can log.

- **`IPoolService.GetAsync`, `InitializeGroupAsync` and `Destroy`.** An addressable item could only
  be fetched through the sub service, because the asynchronous Get was never on the interface; the
  fill of a group could not be waited on; and the destroy surface was on the service but not on
  what a caller injects.

- **Tests for the setter delegate, the injection entries, the pool's config model, a screen's own
  hide-during-show, the function provider's mismatches, and unbinding by instance.** Twenty of them.

### Changed

- **The command path hands the logger its parts rather than a built string.** Every dispatch, every
  command taken and returned, and every sub group built its message before the logger was asked
  whether anyone wanted it. The five `Dispatch` overloads, the binder and the group resolver pass
  the pieces instead, and the one line that pays for an enum's name asks `FlowLogger.IsEnabled`
  first.

- **The binder builds one delegate for the pool return instead of one per dispatch.** Writing the
  method group at the subscription made a new `Action` every time a signal was dispatched, which a
  signal that fires each frame notices.

- **`[Inject]`, `[InjectSignal]` and `[SignalParam]` properties are written through a setter
  delegate.** 1.7.0 said filling a `[SignalParam]` was a typed assignment rather than a `SetValue`;
  the code still called `SetValue`, on every execution of every command. Each entry now carries a
  delegate built once with it - a typed setter in a generic box, so a fill is a delegate call and
  two casts - and falls back to the reflective setter where the box cannot be built.

- **A function runs without reflection and is injected once.** Each shipped arity - `FunctionVoid`
  and `FunctionReturn`, up to four parameters - calls its own typed `Execute`, and a parameter that
  does not fit is reported naming the function and the slot rather than thrown from inside the
  provider. `FunctionBody` remembers the context and binding generation it was filled at, the way a
  pooled command does. The asynchronous completion callback is handed over by the data container
  that knows its type, instead of being found by name on both objects with reflection.

- **The pooled screen instance is taken at Show, not at Open.** `Open<T>()` used to take the
  instance out of the pool before anything was decided, so a builder that never reached `Show` kept
  it out of the pool for good. The builder reads the declaration at Open and asks the pool at Show,
  after the duplication and layer checks - there is nothing to give back on a refusal any more.

- **The screen contexts declare in the four phases like every other context.** `ScreenServiceContext`
  did its binding in `CoreBindings`, which runs when a context starts whatever its Root's phase
  switches say; it binds in `SignalBindings`, `InjectionBindings` and `CommandBindings` now.
  `BaseScreenContext` binds its manager's mediation in `MediationBindings`, and both it and every
  screen context reach the screen service in `Setup`, the phase that may reach another module.

- **Filling a pool group is a Task, and the synchronous Get builds without one.** `CreateSubService.Group`
  returns the fill; `InitializeGroup` starts it and reports what went wrong, where it used to be
  dropped on the floor. The synchronous `Get` builds a direct prefab through `CreateItemSync` rather
  than reading a Task's `Result`, which held only for as long as nothing on the way awaited. The
  `Check` questions answer without warning: a caller asking whether a group is ready is usually
  about to make it ready.

- **`RetryCommand` waits on the coroutine provider**, in real time. It was a `Task.Delay` inside an
  `async void`, which swallowed whatever the retry threw, could not stop with the scene, and never
  fired on WebGL.

- **A screen unloaded from Resources no longer sweeps the heap.** `Resources.UnloadUnusedAssets` ran
  on every single unload, a hitch each time; scheduling that sweep is the game's, on a loading
  screen say.

- **The shared cross-context binder and the two providers are bound once.** Every context asked to
  bind them again and was answered with a warning - three per context, for the framework doing what
  it always does.

- **Names.** `InjectionBinding.BoundContext` and `SetBoundContext` (were `Binded`), `hasSetUp`,
  `AfterStartBeforeLaunchContext`, `AssetSignals.Incoming` and `Outgoing` (were `InComing` and
  `OutGoing`), `PoolItemCVO` (was `PoolItemVO`, and is config data), and `PoolGroupCVO` under
  `Data/ValueObjects/` rather than `Entities/`. `ScreenTag`, `ScreenLoadType`, `FlowRole` and
  `SignalParamDiagnosticKind` carry their numbers, as the rule says every enum does - `ScreenTag`
  is serialized on every Root that overrides a screen. `MediationBinder`'s refusal names `IView`
  rather than an interface that does not exist, and `ScreenSafeArea` logs through the Flow Console.

### Fixed

- **A once-listener added while a once-listener was running was dropped unheard.** `Dispatch` invoked
  the once-list and then cleared the field, so anything the running listener added - including
  itself, added back - was wiped by that line before it was ever called. The list is taken into a
  local and the field cleared first, then invoked. All five arities.

- **A command could be put in the pool twice.** A command that retains and releases inside its own
  `Execute` is back in the pool before that `Execute` returns, so a later step of the same type takes
  the very same instance out again - and the first step's frame, still on the stack, then read the
  second run's flags as its own and returned a running command to the pool. Each execution of a
  pooled instance carries a token now, and the frame checks it before it acts.

- **A command still retained when its group ended was handed to the next dispatch.** It was returned
  to the pool so the pool would see it again, but whatever it was waiting on is still going and still
  holds it: its late `Release` then finished a step of somebody else's run. It is dropped instead,
  which costs one pooled instance and no correctness.

- **The same signal bound twice in one Context threw a null reference.** The base binder answers a
  duplicate key with null, and the second chain's first `ToSequence` dereferenced it - so a mistake
  in a Context was reported as a null reference somewhere inside the framework. It is named now, and
  a binding nothing can reach is handed back so the line the caller wrote still reads and only the
  first chain runs.

- **`PoolConfigModel.GetGroupConfigOfItem` threw on an item nobody registered.** Every pool service
  asked it first and expected null, so the "No GroupConfigKey found" report behind it was never
  reached and a typo in an item key was a dictionary exception. It answers null.

- **`InjectionBinder.UnBind<T>(object)` threw for an instance that was never bound.** It tested the
  wrong variable for null and then read the binding it had not found; a type nobody had bound threw
  a key error on top. A type left with nothing under it is also forgotten, so asking for it is
  answered "nothing is bound" rather than "bound, but not under this name".

- **A view with no `ViewInjector` threw on Register and UnRegister**, and an injector added from
  code and destroyed before its Start threw in `OnDestroy`. A view without an injector is mediated
  and not injected, which is what an injector entry with Injectable View unticked says too.

- **A hide asked for during the show animation was dropped.** The service checks InUse, and a screen
  animating in is InUse as well, so the hide reached the screen and the screen ignored it: the screen
  stayed on stage, and one being unloaded stayed marked as unloading for good. The screen holds the
  hide and plays it the moment the show reports done, and a forced hide clears the animation flag it
  could land in the middle of.

- **A `[Inject]` or `[SignalParam]` property with no setter threw on every injection.** It is
  reported once, when the type's entries are built, and left out.

- **The pool's config commands said "screen configs"**, and the unregister command logged under the
  Screen channel with the register command's name.

### Removed

- `SignalExtensions` - `ConnectWithConversion` and `ConnectEnumToInt`, which `Connect` with a
  converter already does and which registered no disconnector; `SubContextAttribute`, used nowhere;
  the commented-out `PostConstructAttribute` and `DeconstructAttribute` files;
  `InjectionBinderCrossContext.PostConstructedObjects`; and the commented history block in
  `IScreenService`.

## [1.7.1] - 2026-09-07

### Fixed

- **A signal could not be bound again once the Context that owned it was torn down.** 1.7.0 gave a
  signal's command callback an owner, so that a second Context binding the same signal is refused
  rather than silently taking it over. Nothing gave that ownership back: `UnBindAll` dropped the
  bindings and left the callback pointing at a binder that had none, so the next Context to bind the
  signal was refused on behalf of a run that was already over and its commands never ran again.
  Reloading a scene is exactly that - every Context is rebuilt while the signal holder, which lives
  in the cross-context binder, is the same instance it was. The binder now takes its own callback
  off every signal it unbinds, and only its own: a signal another binder has since taken over is
  left alone. The guard itself is unchanged, so two live Contexts still do not share a signal.

- **A command group could be closed by a run that had already finished.** The resolver is pooled,
  and it told its listener the run was over *before* ending it - the listener being the binder
  putting it back in the pool. A sub group finishing therefore parked its resolver while the parent
  carried straight on, and a step of the parent that dispatched a signal took that same resolver
  back out and started a new run on it. The finished run's frames were still on the stack
  underneath, and their `Dispose` then closed the new run mid-flight: its retained command went back
  to the pool with nothing waiting on it, and every step behind that command was never reached, with
  nothing logged. The run is ended before the callback now, so there is nothing left for those
  frames to do, and each run carries an id that the places which carry on after calling out check
  before they touch anything.

## [1.7.0] - 2026-09-07

### Added

- **A one-shot listener can be taken back.** `AddListenerOnce` had no counterpart, so a listener
  waiting for a signal that then never arrived stayed on it for the run. `RemoveListenerOnce` sits
  on all five signal arities and on the interfaces they declare, beside the `RemoveListener` that
  was always there.

- **The Flow Console says how much it keeps and how much it traces.** `CD_FlowConsole` gained
  `StackTraceCapture` - `Never`, `WarningsAndErrors` or `Always` - and `MaxLogCount`, which defaults
  to 5000. Working out where a log came from is the expensive half of logging and only the console's
  source column reads it, so the default captures it for warnings and errors and leaves everything
  else alone. The list is trimmed in blocks of 256 rather than one entry at a time, so a run that
  logs steadily no longer shifts the whole list on every line. Before this the list had no ceiling
  at all: a long play session grew it until the domain reloaded.

- **Sixty-two tests for the parts that carry a flow**, none of them needing a scene: the command
  group resolver, signal listeners and `Connect`, the pool's runtime model, the function provider,
  the shared instance pool, and the screen builder. Two real defects were found by writing them, and
  both are in Fixed below.

### Changed

- **`IScreenService.Open<T>()` hands back an `IScreenBuilder`.** It used to hand back the sub
  service itself, which is one object for the whole run - so the screen being opened lived in that
  object's fields, and two commands opening a screen in the same frame wrote over each other. The
  chain a caller writes is unchanged, and so is every call that reads `Open<T>().Show()`; what
  changes is the declared return type, which a project storing it in a local has to update.

- **A Root holds its sub-contexts by script reference, not by name.** `SubContextData` carries the
  `MonoScript` the context is declared in, so the entry is a real guid in the scene or prefab rather
  than a string nothing in the project depends on. Renaming the class inside its file or moving the
  file now keeps working, and deleting the module a context lives in leaves a reference the Root
  reports instead of a name that quietly stops resolving - which used to be visible only as an error
  at play time, if the scene was ever run. `ContextFullName` stays beside it and is still the only
  half runtime reads, because `MonoScript` is an Editor type and a build nulls the reference; it is
  rewritten from the script whenever the inspector or a generator touches the entry.

  Entries authored before this carry no script. They keep working, and the Root's inspector offers
  **Resolve**, which links one in a press. An entry whose name resolves to nothing offers no button:
  the module is gone, and whether the Root should still list it is a decision.

- **Delete Module takes the module's sub-contexts out of the Roots that list them.** It asks first,
  and there are three answers: remove them from every Root, go through them one at a time, or remove
  none, see where they are and keep the module. This is the rule the rest of the tool already
  follows - unwire a thing from everywhere it is referenced, then delete - reaching the one place
  that used to be exempt, and it is only possible because a sub-context entry is a tracked reference
  now rather than a name nothing depended on.

  The question comes **before** anything is deleted, so cancelling leaves the module whole: its
  assemblies, its settings files and its folder are all still there.

  Which asset holds the Root decides how far it goes. A prefab is a file and is written. A scene
  that is not open is opened, written and closed again. A scene that **is** open is changed and left
  dirty, because whatever else is unsaved in it belongs to whoever opened it - the summary says so,
  and saving is theirs. Every entry removed, skipped or left is named on the console with its Root
  and its asset.

- **A command runs without reflection.** `CommandBody` declares `InvokeExecute`, and each arity
  overrides it to call its own `Execute` - so dispatching a signal no longer does a `GetMethod` and
  an `Invoke` per command, and no longer allocates the parameter array those needed. Filling a
  command's `[SignalParam]` properties is a typed assignment rather than a `SetValue`.

- **Injection is resolved once per type and remembered.** `InjectionExtensions` built the list of
  members to fill on every injection, walking the type's declaration chain and reading attributes
  each time. It builds that list once per type now and keeps it, and the cache is stamped with the
  binder's generation so a rebind invalidates it rather than being missed. The file is roughly half
  the size it was.

- **Context types are indexed once for the run, not looked up per Root.** `RootsManager` owns a
  `ContextTypeIndex` built from the loaded assemblies, so a scene with twenty Roots sweeps them
  once instead of twenty times. The sweep also survives an assembly it cannot load, which used to
  throw `ReflectionTypeLoadException` and take the whole index with it.

- **A pool checkout and a return are constant time.** The runtime model kept its items in lists it
  searched linearly, so a pool of a few hundred cost more on every checkout than the object it was
  saving. Items sit in a set that carries its own index and removes by swapping the last item into
  the gap. The buckets are keyed by pool and by item, which is what makes a lookup a lookup rather
  than a scan.

- **The pool activates what it hands out.** A checkout parents the item and sets it active, rather
  than leaving that to whoever asked. A caller that was already activating the item still works; one
  that assumed it came back inactive has to say so.

- **Four pools of instances become one.** Commands, functions, function data containers, bindings
  and mediators each had their own dictionary of stacks and their own take-and-return code.
  `TypePool<T>` is that code, once, and the double-return guard the mediator pool needs is a
  constructor flag rather than a list walk.

- **Ordinary work stops reporting itself as a warning.** Unbinding something, creating a function
  and returning a container to its pool were logged at warning level, which meant a console filtered
  to warnings filled with the framework doing its job. They log at the normal level; what is
  actually wrong still warns.

- **The Controllers help page shows every binding shape** - sequence, parallel, bind-time
  parameters, `DispatchSignalCommand`, and a group as a step - and both the page and
  `Controller.md` now say where a command's data comes from: the signal reaches the bound method,
  and `[SignalParam]` is filled from the binding's parameters or from the previous step's
  `Release`. Getting that wrong was the most common way to reach a null payload.

### Fixed

- **A group of synchronous parallel steps ran only the first of them.** A group ended as soon as
  nothing was running, and a step that finishes inside its own `Execute` leaves nothing running - so
  `ToParallel` with no async step in it dispatched one command and closed. A group ends when nothing
  is running *and* every step has been reached. Found by the new resolver tests.

- **A group step bound to nothing stopped the chain instead of being skipped.** The resolver waited
  on a step that would never report, so everything after it in the sequence was never reached. It is
  skipped, and says so once.

- **`ResumeContext` paused every sub context it owned.** The loop logged `ResumeContext!` and called
  `PauseContext()`, so resuming a Root left its sub contexts asleep. It also reset three of the
  seven lifecycle flags on teardown, leaving the other four claiming the context had bound and
  launched when it had done neither.

- **`UnBindAll` unbound half the bindings of any key that had more than one.** It counted up to the
  live list's `Count` while always taking element `0`, and unbinding removes from that list - so the
  index chased a shrinking list and stopped in the middle. It works from a snapshot.

- **A screen left the active registers by identity, and is parked once.** Removal matched on type
  and manager rather than on the instance, so with two screens of a type registered the wrong one
  could be taken out; and a screen hidden twice was added to the passive pool twice, which the pool
  then handed out as two different screens.

- **A screen shown or hidden twice carried two subscriptions.** `ShowCompleted` and `HideCompleted`
  were subscribed without being dropped first, so a screen reopened before its last show finished
  ran the completion handler once per open.

- **A screen whose load failed lost the exception.** The load ran as `async void`, so anything it
  threw went nowhere and the screen simply never appeared. It is awaited and caught, and says which
  screen and what went wrong.

- **A command's retain flag belonged to the pooled instance rather than to the run.** A command that
  retained once kept `HasRetain` set when it came back out of the pool, so a later run of the same
  command type was waited on for a `Release` that was never coming. The flags are set at the start
  of every run.

- **An open belonged to the service rather than to the call that made it.** Two opens in flight at
  once shared the sub service's fields, so what one of them was told - its layer, its parameters,
  its force flags - reached the other. Each open carries its own builder, shows once, and returns
  its pooled instance if it is refused.

- **A screen registered nowhere was reported twice**, once by the registry lookup and once by
  `Open`. The lookup is quiet and `Open` is the voice.

- **The mediator pool walked its whole stack to refuse a double return.** It refuses on a set
  instead, so returning a mediator costs the same whether the pool holds two or two hundred.

- **The function provider said "Function Created!" when it had reused one from the pool**, and
  looked up the `Execute` method by reflection on every call. The methods are cached per type and
  the message says which of the two happened.

- **Create Module attached a generated screen to the wrong Root when a module held more than one.**
  The parent's Root prefab was whichever one under its `Prefabs` folder carried a `RootBase` first,
  and `MainModule/Prefabs/` holds `PoolServiceRoot.prefab` beside `MainRoot.prefab` - the right one
  was picked only because the filesystem returned it first. The prefab named after the module wins
  now, matching the suffix a Root actually carries so that `CounterServiceRoot` and
  `PlayerSystemRoot` are recognised as their module's own. A folder with several Roots and none of
  them the module's attaches nothing and says so, because being silently wrong is worse than leaving
  the step to *Add Sub Context*.

### Removed

- `InjectionCaching`, `PostConstructUtils` and `DeconstructUtils` - three files nothing called since
  injection was rewritten around a per-type entry.

- `CommandStepVO.Id`, and the resolver's `_stepsDictionary` and `CommandBinder._activeCommandGroup`
  with it. A `Guid` was generated per step and the dictionaries keyed by it were written to and
  never read.

## [1.6.0] - 2026-09-06

### Added

- **Every module carries a card.** `MODULE.md` at the top of a module says what it is for and what
  it is about in the author's own words, and holds a generated block underneath - kind, assemblies,
  Root and Context, the signals it announces and accepts, the modules under it - written by Module
  Scanner and refreshed after every compile. It is what an agent reads to decide whether a piece of
  work belongs to that module, so `ModuleCardCheck` reports a card that is missing or has fallen
  behind. The two authored lines are asked for in Create Module, at the moment somebody knows what
  the module is for and rarely again afterwards. A test module carries no card: nothing is routed to
  one, and the module it drives already lists it.

- **A `flowioc-screens` skill**, the sixth. It carries the chain a screen opens through, the rule
  that the opening Command fills the screen because it is holding the instance it awaited, when a
  change goes through a signal and when a Command may fetch the screen instead, the Mediator's two
  guards, how an animation reports that it finished, and where a pooled screen is reset.

### Changed

- **`Tools/FlowIoC/Add Shared Data` is now `Add Shared or Signals`**, and offers either assembly to
  a module that already exists rather than only Shared.

- **`MainModule` announces that it started.** `MainSignals.Outgoing.OpenMainScene` was an order
  sitting on Outgoing; it is `Started`, and the Connector joins that to
  `MainScreenSignals.Incoming.OpenMainScreen`, which was already an order and already right.
  MainModule does not decide that opening the main screen is what starting means. `Launch` moved out
  of the public holder into `MainInternalSignals`, which is what it always was - dispatched by
  `MainContext` and handled by `MainContext`. A project that already installed MainModule keeps its
  own copy; this is what a new install gets.

- **How a screen's Mediator guards itself is written down.** It subscribes on `ShowCompleted` and
  unsubscribes on `HideCompleted` - `OnRegister` wires those two and nothing else, because a screen
  is pooled and `OnRegister` runs once while the screen opens many times - and every handler is
  guarded by `ScreenState.AvailableToSendSignal`, so a tap landing during an animation does not
  become a signal.

- **An animation reports when it finished, not when it started.** The state stays `InShowAnimation`
  until the View invokes `ShowCompleted`, which is what that guard is made of. A timeline waits for
  its duration; a set of staggered tweens hangs `OnComplete` on the last one only. And a pooled
  screen is reset in `BeforeScreenActivation` rather than by the hide animation, which is only for
  the look of it.

## [1.5.1] - 2026-09-06

### Added

- **Role ticks the folder it implies.** `Services/` and `Systems/` were already optional folders in
  the main layout and already tickable in the structure panel, but both started clear whatever Role
  said - so choosing Service and finding no `Services/` folder in the module was the ordinary
  outcome. System now arrives with `Systems/` ticked, Service with `Services/`, Core with neither,
  and either tick may still be changed before pressing Create.

### Changed

- **Every enum value carries its number**, and `FolderEVO.FolderType` is written out. Unity
  serializes an enum as an int, so a value inserted or deleted in the middle renumbers everything
  below it and every asset on disk then reads back as the wrong thing. Numbered, a value can be
  deleted without moving the rest. The rule is in the agent rules under Code style.

- **What a Function is, said by what it is for.** The rule read "a Function returns a value and does
  not orchestrate", and neither half survives a real module: the eight Functions in HitNPoP's
  MapModule are all `FunctionVoid`, one of them dispatching signals from inside itself. A Command is
  a step in a flow, read in order; a Function is called from inside one, does its work without
  depending on where it sits, and returns a value only when it has one. The half that holds is the
  trade: a Function is not a step in the Flow Console, so a step somebody should read in the
  sequence is a Command.

- **A decision belongs in a Command, said once.** It was in the documents three times and never by
  itself - a Context needs no `if`, a View holds no `if` about game rules, a Mediator holds none
  either. Those are now instances of it, along with the System type that holds no method doing work.
  With the other half stated too: what needs no decision is not one, so a close button that always
  closes is the Mediator calling `_view.Hide()`.

- **A flow is read from one Context, and a Connector does not decide.** A Command whose only job is
  to dispatch is not written - `DispatchSignalCommand` is bound with the signal and its payload, so
  the signal leaving is a line in the Context. A list of consequences hung off one announcement
  belongs in the sequence that owns it: binding `GameOver` to `OpenGameEndScreen` and to
  `ResetPlayerData` puts both only in the wiring, and reads as though the Connector decided that
  ending a game resets the player's data.

### Removed

- **Three retired `FolderType` values**: `ScreenViews`, `ScreenConfigs` and `SharedSignals`, kept
  alive only to hold their positions. Their numbers - 2, 9 and 24 - are in
  `FolderEVO.RetiredFolderTypes` and are never reused; the heals that removed one type each remove
  all three now, which is the first time `ScreenViews` was removed from anything. A settings asset
  carrying keys for them prunes them on load rather than needing to be deleted.

## [1.5.0] - 2026-09-06

### Added

- **The public signal holder has an assembly of its own.** It moves from `Scripts/Shared/Signals/`
  to `Scripts/Signals/`, which is `Modules.Player.Signals`. A System or a Screen that references a
  neighbour's Shared assembly to read an enum can no longer see that module's signals, so the rule
  that signals cross through a Connector is the compiler's now rather than the reader's. Module
  Scanner gained `SignalReferenceCheck`, which reports any assembly other than a Connector's that
  references a `.Signals`. The folder is mandatory and the assembly is not: a module with no public
  signals leaves it empty and Module Scanner reads that as Ok, which is what `ConnectorModule` does.

- **`FlowRole.Core`.** A Core module is part of the project's frame rather than of its game - one
  per project, a reserved Initialize Order, extended rather than authored. `MainRoot` and
  `ScreenRoot` declare it with `[FlowHeader(FlowRole.Core)]`, and it is the one role a Root can only
  take from the attribute, because a Core module carries no suffix for the name to be read from.
  Create Module writes that line for Role = Core on a main module.

- **A Systems and Services page** in the Help window's Structure category, and a
  `flowioc-systems-services` skill beside the four already installed. Both carry the rules the
  architecture had never written down: a System needs no `System.cs`, one arrives when the module
  wants a surface, it holds injected members and no method that does work, it is built out of sub
  systems, a Model owns the module's state and data while a sub system computes, and a Service is
  driven by a call or by a Command it ships and answers with a signal or a callback.

- **FlowIoC's editor look is offered rather than hidden.** `FlowPalette`, `FlowRowPainter`,
  `FlowHeaderBar` and `FlowHelpPageMap` are public, so a project writing tooling of its own can draw
  a FlowIoC window - the header bar, the row colours, the action button under a list.

### Changed

- **The camera module is `CameraModule`.** A module is named for what it does while its Root and
  Context keep the suffix that says what the Root roots, so the folder is `CameraModule`, the
  assemblies are `Modules.Camera` and `Modules.Camera.Shared`, the namespaces are
  `Modules.CameraModule.*`, and the Root and Context stay `CameraSystemRoot` and
  `CameraSystemContext` - the Context renamed from `CameraContext` so the pair reads together. The
  Help window lists the module as *Camera*. A project that already has the module installed renames
  its own copy: the folder, the two asmdefs and the namespaces they hold.

- **Shared is optional everywhere and starts unticked**, including on a screen module, where it used
  to be forced on. A module pays for that assembly on the day it publishes something.

- **`Services/` and `Systems/` are optional folders**, ticked from *Role* in Create Module: System
  arrives with `Systems/` ticked, Service with `Services/` ticked, and either may be changed.

- **The windows wear FlowIoC's own colour rather than a role's.** The generator windows, the Help
  banner and the Create Module panels were painted with `FlowRole.Root`, which made them all change
  colour the day that fill did. They take a chrome violet no role owns, and their action button
  takes it too - it used to be `Color.cyan`, which belonged to nothing else on screen. Core is
  indigo, deliberately apart from the chrome it sits beside in Create Module's Root preview, and the
  plain Root is red: nothing in a finished project wears it, so it now means a Root that has not
  said what it roots.

- **Folder Painter wears the header bar**, with Refresh and Select Asset on the right of it, and its
  Enabled switch moved under the rules it governs - a tick and the row green while it is on, a
  warning icon and the amber while it is off.

- **Whether a context belongs to a Connector is asked through `FlowRoleResolver` everywhere.** The
  Root inspector was reading the name itself, so a context that declared itself with
  `[FlowHeader(FlowRole.Connector)]` was offered correctly by Add Sub Context and drawn without the
  badge.

### Removed

- **`InputModule`.** It is gone from `Modules~`, from the Help sidebar, and with it
  `InputModulePage`. Nothing in the package referenced it. It will be written again if it is wanted.

- `ScreenHistoryData`, a sub service nothing used.

## [1.4.5] - 2026-09-04

### Added

- **A module's Root and Context are named for what the Root roots.** Create Module asks a main
  module that gets a Root for its *Role*: **System** writes `PlayerSystemRoot` and
  `PlayerSystemContext`, **Service** writes `CounterServiceRoot` and `CounterServiceContext`, and
  **Core** writes the plain `PlayerRoot` and `PlayerContext`. The Root inspector reads a Root's own
  name to colour it, so the choice is what makes a Service module's Root draw as a Service. It
  starts on System, because a module written for the game at hand is one, and the module folder,
  its assembly and its namespaces are untouched whichever is picked.

- **The name field says what it is about to produce.** Beside it sits a preview panel: the folder
  the module will land in, and the Root drawn the way its own inspector will draw it - the role's
  fill, its accent stripe, and the strip naming the assembly and the role.

- **The folder layout is one button away.** The `Config` field left the module type's row for a
  square button on the Folder Structure bar, which selects the layout asset and pings it in the
  Project window.

### Changed

- **Create Module reads as two columns.** Name and preview, type and what the module is made of,
  role and what it publishes, then the parent module beside the folder structure - each row split
  45/55 and growing with the window. The panels wear the Root's violet: a header bar over rows
  tinted a washed-out version of the same, and a screen module's settings wear Screen Scanner's
  amber for the same reason.

- **A gated toggle is shown off rather than hidden.** Create Root and Create Scene stay in the row
  when the toggle above them is off, disabled and unticked, so the row does not lose an entry as it
  is ticked. Create Signals is gone entirely - the public holder lives in Shared and the internal
  one in the Runtime Signals folder, so ticking those folders is the whole answer - and a screen
  module is neither asked nor told about Shared, which it must have.

- **The module tree says what it knows.** A module with nothing under it is drawn without a foldout
  arrow, and `(Screen)` and `(Test)` are the role's own colour rather than words in brackets. The
  parent panel reports a missing parent in its own bar, in place of the second bar that used to say
  so under the list.

- **The other generators match.** Create View, Create Model, Create Command, Delete Module and Add
  Shared Data wear the same header bar, ask for a parent module in the same shape, and keep their
  action button at the bottom of a window that no longer opens too small to use.

- **A screen module cannot nest under another screen module.** It belongs to the module whose
  feature it shows, so Main and Sub host it and Screen and Test do not.

- **The Screens panel wears the header bar every FlowIoC component wears.** It says what the window
  is and which module it belongs to, and carries the window's two ways out on its right: the link
  to the Screens help page, and Refresh. The strip that used to sit under it is gone with them -
  the screen count and the collision count were saying what the rows already say. `FlowHeaderBar`
  grew a `DrawWindow` for this: a window has no fields to explain, so it wears the window's own
  action where a component wears the help toggle.

- **Two screens on one layer are amber rather than red.** The runtime allows it and a game
  sometimes wants it - opening one closes the other - so it is a warning and never a refusal. A
  layer no other screen of that manager opens on is green, which is the settled case rather than a
  checked one.

- **The columns say what they mean.** `Mgr` is `Manager`, and `Show` and `Hide` are `Show Anim` and
  `Hide Anim` with their checkboxes carried to the middle of the column instead of hugging its left
  edge. The headings sit level in their strip too: `miniBoldLabel` is authored for a body of text -
  a 6 pixel top margin over a 3 pixel top padding, aligned upper left - which in a 21 pixel toolbar
  left every word near the bottom of its cell.

### Fixed

- **The Screens panel's Mgr and Layer cells apply when you leave them, not as you type.** Both
  decide where a row sits - the manager picks its box and the layer sorts it inside one - and both
  wrote back on every keystroke, so typing `12` committed `1` after the first digit, moved the row
  into a `Manager 1` box that had not existed a moment earlier, and left the caret pointing at
  whatever row slid into that place. The second digit then landed on the wrong row. They are
  delayed fields now: the value applies on Enter or when the field loses focus. The Tag popup and
  the two animation toggles still apply at once, because a click on either is a finished edit and
  neither moves the row.

- **A `ViewInjector` whose list was never filled says so instead of registering nothing.** The list
  is written by the injector's own inspector, so an object assembled from code reaches `Start` with
  an empty one - or, for a component added at runtime, a null one - and every view on that object
  stays unregistered without a word, with `IsRegistered` quietly false. `Start` now reports which
  views on which object are affected and how to fill the list, and an injector on an object that
  carries no view stays silent, because that one is idle rather than broken.

- **A project's first open no longer opens on a warning about eight issues the install was about
  to fix.** The Module Scan startup report was made from inside `ModuleAutoDetector`, which the
  setup install calls one line before `ModuleRepair.FixAll` - so the scan counted the
  `.csproj.DotSettings` files that had not been written yet and told a brand new reader their eight
  modules had eight issues, on a set FlowIoC had installed itself a moment earlier. The report is
  now the caller's to make: the startup pass reports, and an install reports once its own repair
  has run. `RescanModules` detects and says nothing, which is what a caller in the middle of
  changing the project needs.

- **Create Command wrote a context that did not compile.** The binding it added said `.To<T>()`
  closed with `.InSequence()` or `.InParallel()`, for a binder whose methods are `ToSequence<T>()`
  and `ToParallel<T>()`; it imported `{module}.Commands` while the command lands in `Controllers`;
  it wrote a second `#if UNITY_EDITOR` above a test context's own; and it collected one duplicate
  `using` per run. The block is built in one place now, and a block written by the older generator
  is rewritten into what compiles.

- **Create Command, Create Model and Create View find a context named for its role.** All three
  looked for `{module}Context.cs` by name, so a module holding `PlayerSystemContext.cs` had its
  bindings written to a file that was not there.

- **Removing an injectable or an action no longer breaks the window.** The row was dropped from
  inside its own layout group, which ended the frame with that group still open and left every
  repaint reporting an invalid GUILayout state.

- **Delete Module takes the Shared assembly's project files with it.** The module's own `.csproj`
  and `.csproj.DotSettings` were removed; the ones the Shared assembly leaves at the project root
  stayed behind.

## [1.4.4] - 2026-09-03

### Added

- **A project meeting FlowIoC is shown the Welcome page.** The window opened itself on What's New
  after an update and on nothing at all the first time, which left the one reader who most needed
  a starting point without one: a new project is handed four modules, an `AGENTS.md`, a set of
  agent skills and a Flow Console, and nothing in the Editor says where to begin. The startup now
  opens Welcome on its introduction instead of recording the version in silence, and a project that
  has had FlowIoC for a while still lands on What's New. `WhatsNewDecision.RecordOnly` is
  `Introduce`, because there is no longer a case that records and shows nothing.

### Changed

- **The agent rules are kept up to date without asking.** FlowIoC used to open a modal dialog on
  startup offering to write or refresh the rule block, which was wrong twice over: a modal during a
  load callback blocks every other one behind it - the setup modules install from one, and on a
  first open they waited for the dialog to be answered - and the question had one sensible answer,
  because a stale block describes a version of FlowIoC the project is no longer on. The block is
  generated text between two markers, so nothing a reader wrote is ever touched, which is what
  makes writing it without asking honest. It is now written whenever it is absent or stale, with a
  line in the console saying which file was taken, the same way the agent skills report themselves.
  A project that would rather decide for itself turns it off with a toggle in the Agent Rules
  window, and then nothing is written until Sync is pressed. `Do not ask again`, which lapsed the
  next time the rules changed, is gone - the toggle stays off until it is turned back on.

- **Removing the generated `FlowLogType.cs` is announced rather than done in silence.** Deleting it
  is correct when the settings carry no log types beyond the mandatory channels, because there is
  then nothing to generate - but a deletion that says nothing is indistinguishable from the bug
  fixed in `1.4.3`, where the same call ran on a settings stand-in and took the file with it. The
  cleanup now logs which file it removed and why, and that it comes back as soon as a module or a
  custom channel is registered.

### Fixed

- **The agent rules notice stopped noticing after the first check of a session.** It guarded itself
  with a session flag, and `SessionState` survives a domain reload, so a package update - which is
  a domain reload inside the same session - never re-ran the check: a project that updated twice in
  one sitting kept a rule block describing the version it started on. The check now answers from
  the files themselves, which is the only thing that can be stale, and needs no flag at all.

## [1.4.3] - 2026-09-03

### Fixed

- **Updating the package shows What's New straight away, rather than on the next Editor launch.**
  The notice guarded itself with a session flag so that a domain reload - which happens every time
  a script compiles - would not be mistaken for an update and bring the window back over and over.
  But `SessionState` survives a domain reload and is cleared only when the Editor restarts, and
  updating a package *is* a domain reload inside the same session, so the flag swallowed the one
  event the feature exists for: the reader pressed Update, the new version resolved, and nothing
  was shown until they next opened the Editor. The session now remembers which version it answered
  for instead of that it answered, which still elides a recompile on the same version and no longer
  elides an update to a new one.

- **A project's first open no longer deletes the `FlowLogType.cs` it just generated.** When
  `CD_FlowConsole.asset` cannot be loaded - which happens during the import storm of a first open -
  `FlowLogger` hands back an in-memory stand-in built by `ResetToDefaults`, and that stand-in
  carries the mandatory system channels and no module channels. `FlowLogTypeGenerator` read three
  empty type lists from it and asked `LogTypeSettingsGuard` whether the settings could be trusted;
  the guard answered by counting mandatory channels, which the stand-in has, so the generated file
  was deleted without a word. The next compile then failed with `The name 'FlowLogType' does not
  exist in the current context` in every command that logs, and only focusing the Editor once put
  it right. The settings object now says whether it came from disk, and the guard asks that first,
  so a stand-in is never trusted with a deletion. When it is refused, the console says so rather
  than staying silent.

## [1.4.2] - 2026-09-03

### Fixed

- **What's New is shown to a project that has been on FlowIoC for a while, not only from the
  second update onwards.** The notice remembers the version a reader has seen in `EditorPrefs`,
  and reads nothing recorded as somebody meeting FlowIoC for the first time - who wants the
  introduction rather than a list of changes to a package they have not used. That is right for a
  new project and wrong for every existing one on the day the feature lands, which is how `1.4.1`
  shipped What's New and then could not announce itself to a single reader. The rule now asks the
  project as well as the reader: `ProjectSettings/FlowIoCSetup.json` carries the version the setup
  modules were installed at, so a marker naming an older version is the project saying it has been
  here before, and the notes are shown. No marker, or one naming the version now installed, still
  means a first meeting.

## [1.4.1] - 2026-09-03

### Added

- **A What's New tab on the Help window's Welcome page, and the window opens itself there once
  after an update.** The window has always opened on the introduction, which is right the first
  time somebody meets FlowIoC and wrong every time after a new version lands. The tab is read out
  of the `CHANGELOG.md` the package ships, reduced to one line per entry: every entry's first
  sentence, grouped under the version and section it was written in, newest first. Nothing is
  written twice, so a release still means one file to update. The version a reader has seen is
  remembered per person rather than per project, because a marker committed with the project
  would be ticked by whoever updated the package and nobody who pulled afterwards would ever be
  shown the notes.

- **Add Sub Context says what kind each listed context is, and where it is already used.** The
  window now wears the same `SCREEN` and `CONNECTOR` badges the Root's own list does, so a context
  is recognisable before it is added rather than after. A context that a Root in one of the open
  scenes already lists says which Root has it, and sorts to the bottom of the list instead of
  disappearing from it - adding the same screen context to a second Root with a second `ManagerId`
  is a real thing to want, and hiding the row would have made it look impossible.

### Fixed

- **An error reaches the console whether or not `ENABLE_LOG` is defined, and exactly once.** Every
  `FlowLogger.LogError` overload now routes through one `WriteError` that carries no
  `[Conditional]` and consults no setting, so an error survives both a missing `ENABLE_LOG` define
  and `IsLoggingEnabled` being off - a build that had turned logging off was throwing its
  diagnostics away with it. Errors also take an optional context object, so clicking one in the
  console selects the object it is about, and the three runtime `Debug.LogError` call sites moved
  onto the matching channels; the last `Debug`/`FlowLogger` pair among them had been printing the
  same error twice.
- **`InputRoot.prefab`'s view registers again, and both shipped scenes hang their views off a
  Root.** `InputView` sat on the Root's own GameObject, and bubbling up starts at the view's parent
  rather than at the view itself, so the only way it ever worked was the *Use Root Selection*
  toggle the `ContextSource` rework retired. The view moved to an `InputView` child under
  `InputRoot`, where bubbling up finds the Root with nothing configured. The counter module's test
  scene was re-authored the same way, after `LocalSaveTestScene`: the `Canvas` carries the
  `ViewInjector` and the View itself, and it and the `EventSystem` sit under the test Root, which
  is what the rule about a module's GameObjects living under its Root asked for all along. Every
  other shipped scene and prefab was reserialized onto `ContextSource` in the same pass, so no
  asset FlowIoC ships still carries the retired `UseBubbleUp` and `UseRootSelection` keys.
- **A View that names its Root now actually registers against it.** The injector resolved a
  view's context from its entry when the object started, and registration then threw that answer
  away and looked at the selected Root alone. A view with *Use Bubble-up* off and a Root Name
  typed therefore waited for the named context and registered against whatever Root happened to
  sit above it in the hierarchy - or reported that there was none. `ViewInjector.ResolveContext`
  is now the one place that answers the question, and `ViewExtensions.Register` asks it. A Root
  name that no Root answers to is reported and the object carries on, where
  `RootsManager.GetRootByName` used to throw a `KeyNotFoundException` on a typo.
- **Two views on one object waiting for different Roots no longer strand one of them.** The
  injector subscribed to `OnContextReady` once per view and unsubscribed once per handler run, so
  the first Root to become ready took away the subscription the other view was still waiting on.
  It now subscribes once and lets go only when every view on the object has its context.
- **The inspector's Register button works on a view that does not register itself.** *Auto
  Register* says whether the injector registers the view on its own; it was also consulted by the
  registration itself, so pressing the button by hand did nothing and said nothing.

- **A generated module's signal holders no longer keep `Scripts` in their namespace.** *Create
  Module* wrote `Modules.PlayerModule.Scripts.Shared.Signals` while every module already on disk
  said `Modules.PlayerModule.Shared.Signals`, so Rider reported the namespace as not matching the
  file location and a namespace reorganise would have renamed a type nobody meant to touch. The
  generator wrote its skip list into each assembly's `.csproj.DotSettings` and then read it back
  from a project-wide `Project.DotSettings` that has never existed, so every folder counted as a
  namespace provider. Both sides now answer from `DotSettingsPlan`, the one list the settings files
  are written from, and the folder-to-segment decision moved into `FolderNamespaceSegments`, which
  is covered by tests that need no project on disk.

### Changed

- **`CountdownServiceModule` is now `CounterModule`, and its service counts in both directions
  under names that say so.** The module always measured elapsed time as well as counting down, so
  a name that described only half of it was the wrong name. **This is a breaking change**: the
  folder is `CounterModule`, the assemblies are `Modules.Counter`, `Modules.Counter.Shared` and
  `Modules.Counter.Test`, the namespaces are `Modules.CounterModule.*`, and the types are
  `ICounterService`, `CounterService`, `ICounterModel`, `CounterModel`, `CounterVO`,
  `CounterRequestVO` and `CounterServiceSignals`. `EvaluateElapsedTime` is now `CountUpFrom`, to
  read beside `CountDownFrom`, and the request's `CountdownTick`, `CountdownComplete` and
  `CountdownStop` are `CounterTick`, `CounterComplete` and `CounterStop`. The Root and Context
  keep the suffix - `CounterServiceRoot` and `CounterServiceContext` - because a Root takes the
  colour of whatever it roots and reads that from its own name, which is now written down as a
  rule in the agent rules, the README and the Help window.
- **Add Sub Context offers only the contexts that may honestly go on a Root.** A context that some
  Root declares as its `Root<T>` is built by that Root already, so adding it to a second Root would
  build a second instance and run the same bindings twice - those are now hidden. A module meant to
  be hosted on another module's Root says so with the new `[AllowAsSubContext]` on its context and
  is offered again, and `[ExcludeFromContextWindow]` says the opposite and wins over it. A
  Connector sub-context is offered on the Connector Root and nowhere else, so the wiring between
  two modules stays in the one place it belongs; a context counts as a Connector's when its name
  says so or when it carries `[FlowHeader(FlowRole.Connector)]`. *Create Module* offers
  `[AllowAsSubContext]` as a toggle on a main module that gets a Root, unticked, because a module
  with a Root of its own is the ordinary case.
- **A view says where its context comes from with one value instead of two toggles.** *Use
  Bubble-up* and *Use Root Selection* are replaced by `ContextSource`, which is `BubbleUp`,
  `SelectedRoot` or `RootName`. The two booleans could say both things at once and the inspector
  had to hide one of them to keep the answer single. **This is a breaking change**: an entry that
  selected a Root is read back as bubbling up, because the old toggles are gone from the
  serialised data. The two shipped entries that selected a Root have been re-authored to bubble up
  instead of migrated, because a view that cannot find its Root by bubbling up is a hierarchy
  problem rather than a setting; every other entry FlowIoC ships was already bubbling up. A project
  that had a view selecting a Root has to pick it again.
- **The View Injector's inspector was rewritten in the same visual language as the rest.** One
  card, one folding entry per view wearing a badge that says where its context comes from, and a
  `?` beside every field that opens what that field does - read from the documentation
  `ViewInjectorData` now carries, so nothing is written twice. Selecting a Root inside a prefab
  warns that a prefab cannot hold a scene reference, which used to fail silently when the asset
  was saved. While the game runs each entry reports the Context it reached and whether it
  registered, with the badge and the single action the Root's own lifecycle table uses; the red
  and green buttons are gone. Edits go through the serialized object, so they are undoable and
  land on the prefab or scene that holds the object.
- **`AssetServiceRoot` moved from `-10000` to `-1`**, the last seat in the Services band. It was
  the one shipped Root still carrying a number from the old open-ended scheme, so Initialize Order
  now runs from `-100` to `100` with nothing outside it, exactly as the agent rules, the README,
  the `flowioc-root-order` skill and the Help window's Ordering Roots page all say. Nothing binds
  differently: the asset service still comes up ahead of every module that injects it, and no
  shipped scene overrides the value. A project that placed a Root below `-1` on purpose, to bind
  before the asset service, should check that it still does.

## [1.4.0] - 2026-09-02

### Added

- **Initialize Order is a documented convention rather than a number people guess at.** The Roots
  the package ships fall into bands - Services take negative numbers because they depend on
  nothing, the game's own modules take `0` to `97`, `ConnectorRoot` takes `98` so the wiring reads
  after the modules it wires, `ScreenRoot` takes `99`, and `MainRoot` takes `100` because its
  `Launch` is the entry point. The bands are now written down in the agent rules, in the README's
  lifecycle section, in a new **Ordering Roots** page in the Help window - with a picture of
  `MainScene` showing the Hierarchy authored in the same order - and in a `flowioc-root-order`
  skill installed alongside the others. The page's numbers are checked against the
  `initializeOrder` actually serialised into the shipped prefabs, so the two cannot drift apart.
- **What each lifecycle phase is for, written down.** The binding phases declare, `Setup`
  initialises - it does not run until every Root in the scene has finished binding, so it is where
  a module readies its Models and the only phase that may reach across modules - and `Launch`
  dispatches the first signal. In the agent rules, the README, and the Help window's Root & Context
  page.
- **A `flowioc-connectors` skill**, covering the shape of a Connector sub-context, the `Connect`
  overloads, connection groups, and what to check when a signal is dispatched but never arrives.
- **One visual language across every inspector.** A FlowIoC component now wears a bar that says
  what it is and which module it comes from, coloured by its role: Root purple, Service blue,
  System teal, View orange, Mediator its darker shade, Screen amber, Connector green, Adapter and
  Test grey. Each colour is two values - a deep one the bar is filled with, dark enough that the
  white title clears 4.5:1, and a vivid one for the stripe down its left edge and the help accents -
  and the palette is covered by a test, so a colour that fails contrast or disappears against a
  skin cannot be committed.

  A Root takes the colour of whatever it roots, which is why `ScreenServiceRoot` reads as a Service
  and `ConnectorRoot` as a Connector while a game module's Root stays a Root. A Service, a System
  and a Connector sub-context are not components, so without that rule three of the colours would
  never have been seen at all; the sub-context list badges connector and screen entries for the
  same reason.
- **A help button on every documented field, reading the code's own `///` summary.** Nothing is
  written twice: the parser takes the summary above a field, follows the type's base chain, and a
  field with no comment simply has no button. The Root's fields are documented for it, so the
  lifecycle table explains itself. Where Odin is installed the button joins Odin's own drawer
  chain, so `[Button]`, `[ShowIf]` and the rest keep drawing exactly as before.
- **`[FlowHeader]`**, for a type whose role the package cannot read from the type itself. It also
  takes an optional label, so a type can wear a role's colour without claiming to be one -
  `ViewInjector` wears the Mediator colour and says `VIEW INJECTOR`.
- **`Tools/FlowIoC/Inspector Bar`**, which turns all of this off for a project that wants its
  inspectors untouched.

### Changed

- **One *Module Scan* panel replaces the three tools that used to repair a module.** *Assembly
  Creator Window*, *Module Configuration ▸ Detect & Fix Module Index* and *Module Configuration ▸
  Update Namespace Settings* are gone. Between them they had a dependency order nothing stated -
  the namespace settings silently skip a module whose index entry is stale or whose assembly is
  missing - and none of them could say what was actually wrong. Assembly Creator could not fire at
  all: its checkbox was disabled whenever a module already had an asmdef, and *Create Module*
  always writes one.

  The panel reads every module from the folder tree rather than from the index, so its rows are
  right even when the index is not, and reports nine things: mandatory folders, the Shared
  assembly, the assembly definition, its references, the root `.csproj.DotSettings`, and for the
  project itself the module index, orphaned settings files, the Flow log types and the solution
  code style. `Fix All` repairs everything that does not require renaming an assembly or removing
  a reference someone added by hand; those rows stay red and say what to do. If anything is wrong,
  the console carries one line about it on editor load.

- **BREAKING: a screen declares itself in its context. The `CD_Screen` asset is gone.** A screen
  module's context now derives from `ScreenSubContext<TView, TMediator>`, which binds the View to
  the Mediator, and declares where the prefab lives and how the screen behaves in a `ScreenCVO` -
  `Load = ScreenLoadCVO.Addressable("SettingsScreen")`, `Layer`, `Tag`, `ManagerId`, the two
  animation flags. The context registers the screen with the service in `Setup`; when the screen is
  instantiated the service tells the prefab's `ViewInjector` which context owns the view
  (`ViewInjector.AssignContext`), so the mediator comes from the context that bound it instead of
  being reflected into `ScreenRoot`'s context from strings in an asset.

  Gone with the asset: the type-name strings and the AppDomain scan that resolved them, the
  `ScreenConfigAdapter`, the `ScreenManager`'s config list, the `DirectPrefab` load type (a context
  cannot hold a prefab reference; `Addressable` and `Resource` remain), and
  `Tools/FlowIoC/Screen Config Manager` with its Help page. `Create Module` writes the context, adds
  it to the parent module's Root prefab, and the screen's test Root lists that same context instead
  of re-declaring the screen.

  Upgrading: on the first Editor open, the package generates a context for every `CD_Screen` asset
  whose screen has none, attaches it to the parent Root where it can find one, and asks before
  deleting the assets. A screen that already has a hand-written context, or that used
  `DirectPrefab`, is reported with the `Screen` block to paste, because that file is yours. The
  legacy `CD_Screen` type is kept editor-only for this, and goes in the following release.

- **BREAKING: every ScriptableObject the package ships now carries the data-type prefix its own
  rules ask for.** `CD_PoolGroup` already followed the table; the rest did not. The editor-only
  assets became `ED_CodeGenerator`, `ED_ModuleIndex`, `ED_FolderPainter` and
  `ED_MainModuleDirectoryStructure` / `ED_ScreenModuleDirectoryStructure` /
  `ED_TestModuleDirectoryStructure`, and the config a game reads at runtime became
  `CD_FlowConsole`. The serializable classes they are built out of took the
  matching suffixes: `FolderEVO`, `ModuleDescriptorEVO`, the nine `FolderPainter*EVO` rule types,
  and `CD_FlowConsole.FlowConsoleLogTypeCVO`.

  `FlowConsoleSettings` was public, so code that names it has to be updated to `CD_FlowConsole`.
  Nothing else about it changed.

  The asset files were renamed with the types. The assets FlowIoC writes into a project are moved
  by the path migrator on the first editor tick after the upgrade, keeping their GUIDs and
  everything configured in them, so a project keeps its log types, folder colors and generator
  settings.

- **BREAKING: the Folder Drawer is now the Folder Painter.** The menu item is
  `Tools/FlowIoC/Folder Painter`, the types are `FolderPainter`, `FolderPainterWindow`,
  `ED_FolderPainter` and the nine `FolderPainter*EVO` rule classes, and the namespace is
  `FlowIoC.Editor.FolderPainter`. Unity already calls a property drawer a drawer, and the tool
  paints folders rather than drawing them, so the old name read as the wrong thing twice over.
  The project-local config moves from `Editor/FolderDrawer/ED_FolderDrawer.asset` to
  `Editor/FolderPainter/ED_FolderPainter.asset` on the first editor tick after the upgrade,
  keeping its GUID and every rule configured in it.

- **A Connector now gets signal holders instead of binding them.** `InjectionBinderCrossContext`
  `.GetInstance<PlayerSignals>()` rather than `.Bind<PlayerSignals>()`, in the shipped
  `ConnectorModule` and in every example. `Bind` is get-or-create, so a Connector wiring a module
  whose Root is missing from the scene used to receive a fresh holder, connect to that, and
  silently deliver every signal to nobody. Module Contexts still bind their own holder as before.
- **`InjectionBinder.GetInstance<T>` reports a missing binding instead of throwing.** It used to
  index the container directly and hand the caller a bare `KeyNotFoundException`; it now names the
  type - and the name it was asked for, if any - and returns the default.
- **`ConnectorRoot` moved from `101` to `98`.** The frame barrier already guarantees that every
  signal holder is bound before any `Setup` runs, so the Connector never needed to be last; `98`
  places it after the modules it wires and before the screen host and the entry point.
- **A Root's inspector is one lifecycle table.** Six option boxes and a status box that repeated
  what they said became a single card: a row per phase, with the toggle that says whether it
  happens on its own and - while the game runs - a badge for what has happened and a button to make
  it happen now. Two bugs went with them: the *Bind Mediations* toggle wrote to
  `AutoBindInjections` and so never saved, and a derived Root drew its own fields twice. Removing
  an entry from the sub-context list now records an Undo, which it did not before.
- **The shipped Roots are named after what they root**, so their colours follow from their names:
  `CameraRoot` is `CameraSystemRoot`, `GameplayRoot` is `GameplaySystemRoot`, and the pool prefab
  drops its `Variant` suffix.
- **The help window's banner comes from the inspector palette.** It also gets darker: white on the
  purple it used to be measured 3.7:1.

### Removed

- **The `Assets ▸ FlowIoC ▸ Create Assembly` and `Assets ▸ FlowIoC ▸ Update Module's Namespaces`
  context menus.** Both wrote `.csproj.DotSettings` inside the module folder, where Rider never
  reads it - the file has to sit beside the `.csproj`, which is at the project root - so neither
  had any effect. Rewriting the `namespace` line inside `.cs` files goes with them: once the
  settings file at the root is right, Rider's own *Adjust Namespaces* does that better, moving
  files and fixing `using` directives as it goes.
- **BREAKING: `CustomClassHeaderAttribute` is gone, and with it the header drawer nothing called.**
  The attribute described a gradient bar in colours each type chose for itself, which is the
  opposite of a shared language; a role decides the colour now, and `[FlowHeader]` is there for the
  types whose role cannot be read from the type. `CustomHeaderDrawer`, `CustomHeaderEditor`,
  `GenericCustomHeaderEditor` and the `ED_Header` asset they used were never called from anywhere
  and go with it.

  Upgrading: delete the attribute from your own types. Most need nothing in its place - a Root, a
  View and a screen are recognised on their own.

## [1.3.1] - 2026-08-31

### Added

- **An Input module, installed from the Help window.** It turns the pointer into signals -
  `PointerPressed`, `PointerDragged` and `PointerReleased`, each carrying the screen position - so
  nothing in a game has to read the Input System itself. Dragged is announced only while the
  pointer is down, because a signal per mouse move would be a dispatch per frame for something
  almost no game wants. `SetActionMapEnabled` turns a map on or off by name, which silences input
  at the source rather than leaving it to be ignored downstream. The module brings an action asset
  of its own with a Pointer map bound to mouse and to touch, read from the prefab rather than from
  the project wide actions, so a game can point it at an asset it owns. It is a ready-made module
  rather than part of the setup set: a scene's EventSystem is an ordinary Unity component, not a
  reason to install a module.

### Fixed

- **The setup set had no `EventSystem`, so nothing in `MainScene` could be pressed.** uGUI received
  no pointer input at all, which is why the three difficulty buttons did nothing. `MainScene`
  carries an ordinary EventSystem now. The component beside it matters as much: the generated test
  scenes used `StandaloneInputModule`, which reads through the legacy `UnityEngine.Input` and throws
  in a project whose active input handling is the Input System alone - what a new Unity 6 project is
  set to. FlowIoC looks the Input System's own module up by name, so no package reference is added
  to the Editor assembly and a project without `com.unity.inputsystem` still compiles, and falls
  back to the legacy module when the package is absent.

- **The setup modules could not install into a project that had never had FlowIoC in it.** Without
  a `CodeGeneratorSettings` asset - which until now only the generator menus created - the module
  index was left empty, `FlowLogType` was written with no channels, and every module the set had
  just installed referred to a channel that did not exist. The project would not compile and the
  Editor would not enter play mode. The settings asset is created before the set is registered now.

### Changed

- **A module ships whatever it hands the reader, and adds nothing to the `Tools/FlowIoC` menu.**
  The countdown module's test scene was built by a menu item; it is an ordinary asset in the
  payload now, the way `MainScene` already was, and the `Tools/FlowIoC/Modules` branch that held
  that one item is gone. The payload carries a `.meta` beside every script it references, so the
  scene's references resolve in a consuming project.

- **The Editor's own `SerializableDictionary` is gone**, replaced by
  `UnityEngine.Rendering.SerializedDictionary`, which the package already depended on. The two
  types serialize under different field names, so a settings asset written by an earlier FlowIoC
  arrives with its maps empty; `CodeGeneratorSettings` refills a map that comes back empty from the
  defaults and leaves one that still holds anything alone, because removing a single entry in the
  inspector is a considered act.

- **`MainScene` no longer brings a Global Volume**, and `GameplayRoot`'s initialize order lives on
  the prefab rather than as an override on the scene instance.

- **Two rules were added to the agent rules, the README and the Help window.** A GameObject a
  module needs in the scene goes under that module's Root; and a module whose work outlives a scene
  detaches its Root and marks it do-not-destroy in `BeforeCreateContext`, where the reparenting is
  not decoration - Unity marks only root level objects as do not destroy.

## [1.3.0] - 2026-08-31

### Fixed

- Module assembly names are built in one tested place. Four hand-rolled copies read the suffix off
  a module folder without asking what stood in front of it, so a module named exactly
  `ScreenModule` matched the screen rule with an empty parent and came out as `Modules..Screen`.
  An empty parent falls through to the plain module rule now, and the `ScreenTestModule` branch no
  longer strips twelve characters of a sixteen character suffix.

- The code style FlowIoC ships now reaches a project on its own. The naming rules that decide what
  a `CD_` asset or a `PVO` value object may be called live in the solution level settings file, and
  `Tools/FlowIoC/Module Configuration/Update Namespace Settings` was the only thing that ever wrote
  it - so a project that installed the package and generated a module had every convention
  documented and none of them enforced. The file is written when the Editor opens now, the way the
  agent rules and the skills already are. Only the keys FlowIoC ships are touched, so a team's own
  settings survive, and a session that finds the file already correct writes nothing and says
  nothing.

- The package's own assemblies declare their root namespace, so an IDE stops reporting every
  file in FlowIoC as living in the wrong namespace. Rider derives the expected namespace from
  the folder path below the assembly definition and had nothing to put in front of it, so
  `FlowIoC.BaseModule.Controller` read as one segment too long. A registry install cannot be
  fixed the way an embedded one was - the folder rules a project keeps name a path, and under
  `Library/PackageCache` that path carries a hash that changes with every version - so the
  answer belongs in the assembly definition, which travels with the package.

- A test module is wired to the module it sits under. `Create Module` gave a test module a
  reference to FlowIoC and to its parent's Shared assembly, but not to the parent itself, so the
  first thing a test module tried to test would not compile until somebody edited the asmdef by
  hand. A test module is the one kind allowed to reach anything, so it now gets the reference
  outright. Every other module type is unchanged: reaching a neighbour's Models and Commands is
  still the thing the architecture does not allow, and Shared is still how data crosses.

- A test module's generated signal holder is wrapped in `#if UNITY_EDITOR`, the way its Root and
  Context already were. It was the one file `Create Module` left unguarded, which was enough to
  carry the whole test module into a player build.

- `ModuleDeleter.DeleteModule` no longer opens a dialog of its own. It returns the list of what it
  removed and leaves announcing to the caller, so the Delete Module window still shows its summary
  while a batch script, an editor test or a tool driving the Editor can delete a module without a
  modal blocking the Editor until somebody clicks it.

### Changed

- A module publishes its signals through Shared. The public holder lives in
  `Scripts/Shared/Signals/` and compiles into the module's Shared assembly, so a Connector reaches
  a module through `Modules.X.Shared` alone and no module's assembly ever has to reference
  another's. What a public signal carries moves to Shared with it. The module's own traffic gets a
  second holder, `XInternalSignals` in `Scripts/Runtime/Signals/`, with no `Incoming` and no
  `Outgoing`: those two halves describe a boundary, and an internal signal never crosses one.
  Screen modules therefore get a Shared assembly of their own, and the Create Shared toggle starts
  ticked for them.

- Screen views and mediators are generated straight into `ViewsMediators/`; the `ScreenViews`
  folder is gone. A generated screen view wires its buttons in `OnEnable` and drops them in
  `OnDisable` rather than in `Awake`, because a screen is pooled - `Awake` runs once while the
  screen opens many times.

- The Help window's sidebar nests. A category holds sections rather than pages, so Structure and
  Editor Tools fold out inside Wiki instead of competing with it at the top level. Closed, the
  sidebar is now three entries - Welcome, Wiki, Modules - and a reader picks the part of FlowIoC
  they want before picking a topic in it. Which categories are open is remembered by the path down
  to them rather than by their name, so two modules may both bring a category called Usage. The
  sidebar is wider and keeps its scrollbar's width reserved, so a topic two levels down fits its
  name on one line and the list no longer shifts under the cursor the moment the scrollbar appears.

### Added

- **The setup modules.** A project that gets FlowIoC and has no modules of its own is given the six
  a game starts with, the first time the Editor opens on it: `MainModule` launches the game and
  owns `MainScene`, `ScreenModule` holds the ScreenManager and the layers screens open into,
  `ConnectorModule` is where the modules meet, `GameplayModule` is the game, and
  `MainScreenModule` and `GameplayScreenModule` sit inside their parents. Together they are a flow
  that already runs - the game launches, the main screen opens, picking Easy, Medium or Hard closes
  it and opens the gameplay screen with the difficulty carried as a signal parameter - so a reader
  can see how FlowIoC is wired rather than be told.

  There is no button and no dialog. The set arrives registered: the module index, the `FlowLogType`
  channels, the `.csproj.DotSettings`, the Addressables entries for both screens, and `MainScene`
  at the front of the build list and open. It installs all of it or none - the payload holds GUID
  references that cross module boundaries, so half a set is a set that does not work, and every
  target is checked before anything is written.

  It happens once. `ProjectSettings/FlowIoCSetup.json` records that it did and belongs in source
  control, so delete one of the modules and it stays deleted. A project that already had modules of
  its own when FlowIoC arrived is marked and left alone - having them is a decision - and can still
  take the set from the button on the Setup Modules page in the Help window. A batch run writes
  nothing at all.

- **CameraSystemModule.** Named Cinemachine cameras switched by signal, per-camera last positions
  and custom blends as data, with two adapters that register a rig's cameras on their own.
  Installing it copies its own folder and nothing else, so its Help page carries the connector
  sub-context to write by hand. Its assemblies reference Cinemachine and the render pipeline core,
  which a project may not have: what is absent is worked out and added first, and the intent
  survives the domain reload the resolve usually triggers.

- Shared has a toggle of its own in `Create Module`, beside Create Signals, rather than only being
  reachable by ticking its folder in the preview tree. It starts off for a plain module - one that
  hands nothing to its neighbours has no use for a second assembly - and ticked for a screen
  module, which publishes its signals through Shared. `Tools/FlowIoC/Help` becomes Welcome, Wiki
  and Modules, each opening the window on the first topic of that section rather than always on the
  introduction.

- Ready-made modules, installed from the Help window. A module the package ships lives in
  `Modules~/`, which Unity does not import, and the Install button on its Help page copies it into
  `Assets/Modules/` - then registers it in the module index, gives it its `FlowLogType` channel and
  writes the `.csproj.DotSettings` its namespaces need. Copying the folder by hand gets the files
  and none of the other three. Whether a module is already installed is decided by the assembly it
  declares rather than by the folder it landed in, so renaming that folder or moving it out of
  `Assets/Modules/` - which a game is free to do, the module being its code now - does not offer to
  install a second copy on top of it. A module already in the project is never overwritten: the copy there
  is the one the game has been editing.

- The first of them: **CountdownServiceModule**. Named countdowns with once-a-second callbacks,
  seconds left or 0..1, several listeners on one id, counting up as well as down, and an
  `ITimeSource` behind it so a server clock can replace the device one without touching a call site.
  It ships with its test module, which is also the worked example.

- A Modules category in the Help window, and the first page in it: Countdown Service, with what the
  module does, a Usage tab of worked calls, and a Time Source tab on swapping the device clock for
  a clock the player cannot move.

- A page in the Help window can offer one action, drawn as a button on the right of its banner.
  Install is the first; the tabs beside it change what you are reading, and this changes the
  project.

- A second agent skill, `flowioc-scaffolding`, installed into `.claude/skills` alongside
  `flowioc-data-types`. The agent rules say not to lay a module out by hand; this is the part that
  did not fit in them - which menu item does what, what to fill in, why the optional folders are
  the step that is easiest to get wrong, where the `.csproj.DotSettings` files land and why, and
  how to drive the generators from a terminal against an open Editor.

### Removed

- The Command Execution Test Module sample, and the Samples tab it was imported from. Ready-made
  modules cover the same ground better: they install from the Help window, land somewhere a game
  can actually use them rather than under `Assets/Samples/<package>/<version>/`, and arrive
  registered instead of as loose files.

## [1.2.0] - 2026-08-27

### Added

- The Help window's Wiki opens on a new *Creating a Module* page, ahead of the folder layout
  and the data types, because generating the module is what a reader does before either
  convention applies. It carries a screenshot of `Tools > FlowIoC > Create Module`, says why a
  module is generated rather than made by hand, and explains what the Main, Screen and Test
  types each produce, where they are allowed to sit and what the toggles beside them decide.
  Help pages can show a picture now: screenshots live in `Editor/Help/Images` and are drawn
  through `HelpPainter.Image`, scaled down to the page and captioned.

- The extension methods that lived in the separate FlowIoC-addons package now ship with the
  core package, under `Runtime/ExtensionModule` and the `FlowIoC.ExtensionModule` namespace:
  vector and float maths, enum flag enumeration, list conversion and UTC time formatting.
  `SafeSerializedScriptableObject` and the enum modifier built on it need Odin Inspector, so
  they compile only where `ODIN_INSPECTOR` is defined and the package gains no dependency of
  its own. The addons package keeps its copies under `FlowIoC.Addons.Extensions`, so a project
  that has both installed still compiles.

- The data type convention is written down. A module's data carries its origin in its name:
  `CD_` for config a designer authors, `RD_` for what play produces, `PD_` for what the save
  system keeps, `ED_` for editor tooling and `DD_` for a copy of what a backend owns, each
  with the matching `CVO`, `RVO`, `PVO`, `EVO` and `DVO` suffix on the value objects inside
  it. A value object that carries two kinds at once - an authored half and a runtime half in
  one place - is named after neither and keeps the lettered suffixes on its parts. The rule is
  in the agent rules, in the README, and on a new *Data Types* page in the Help window.

- The skills FlowIoC ships are installed into the project's `.claude/skills` folder when the
  Editor opens, one folder per skill, and refreshed when the package changes one. What gets
  written is logged rather than done silently, and a batch run installs nothing.
  `Tools/FlowIoC/AI/Agent Skills` shows what is there and puts a deleted skill back without
  waiting for the next session. Removing FlowIoC through the Package Manager takes the shipped
  skills with it, file by file, so nothing the consumer wrote is caught up in it: a skill of
  their own, or a note left beside a shipped one, keeps its folder. A removal that raises no
  event - `manifest.json` edited by hand, the folder deleted - is covered the way the rule block
  covers it: every shipped skill opens by saying it applies only while FlowIoC is installed.
  The agent rules are what an assistant is told on every task and so stay short; a skill is what
  it reaches for when one kind of work comes up and can afford the detail. The first is
  `flowioc-data-types`. Only the files the package owns are compared, so a skill you wrote
  yourself is never touched and a note left beside a shipped one survives an install.

- *Create Module* writes a module's signal holder. `Signals` is a folder type of its own now
  rather than a plain folder, so the generator can find it; a new `TempSignals` template
  produces `<Name>Signals` with its `Incoming` and `Outgoing` classes, the Context declares
  and binds it in `SignalBindings`, and a screen Mediator gets it injected. Mandatory for a
  Screen module, which has no Context of its own and so reaches the outside world only
  through its holder; a `Create Signals` toggle for Main and Sub modules, on by default;
  never offered for a Test module. A project whose `DirectoryStructureConfig` assets predate
  the folder type has its `Signals` folder still typed as a plain folder — retype it in the
  asset's inspector, or the generator cannot resolve it.

- A shared code style. `Tools/FlowIoC/Module Configuration/Update Namespace Settings` now also
  writes `<Solution>.sln.DotSettings`, so every project FlowIoC is installed in gets the same
  ReSharper and Rider naming rules, prefixes and spacing. Rider reads only a settings file named
  after the solution and that name differs per project, so the file is generated rather than
  shipped. Only the keys FlowIoC owns are written; anything else in the file survives, and a
  settings file left behind by a renamed solution is removed. The agent rules point at the file
  so agents write code that matches it.

- `Tools/FlowIoC/Help`, an Editor window that introduces the architecture without leaving
  Unity. It opens on the module folder layout — every folder annotated with what belongs in
  it — and the topics beside it walk one diagram at a time through the Root and Context, the
  signal surface, commands and functions, models, views and mediators, and connectors. Each
  step lights up a box, states the rule it stands for, and shows the code that rule produces.
  The window teaches one worked example and never inspects the project it is opened in.

- `FlowIoCModuleIndex.asset` is kept out of version control. FlowIoC writes a `.gitignore` next
  to the asset covering it and its `.meta`. The index is a cache the next rebuild reproduces, so
  two people adding modules on separate branches were meeting in the same serialized file for
  nothing. The rule goes in that folder rather than in the project's root `.gitignore`, which
  belongs to the project: a `.gitignore` only reaches the directory it sits in and below, and
  this is the folder FlowIoC owns and the path migrator can move. Lines outside the
  `FLOWIOC:BEGIN`/`FLOWIOC:END` markers are left alone, the same way the agent rules block works.
  A project that already committed the asset keeps committing it until it runs
  `git rm --cached Assets/Plugins/FlowIoC/Editor/CodeGenerator/FlowIoCModuleIndex.asset*` once —
  `.gitignore` does not untrack what git already tracks.

### Changed

- The Help window sidebar folds *Folder Layout* into a *Wiki* category, alongside the new
  *Data Types* page, and the window now opens on *Welcome* rather than on the folder layout - a
  reader who has never seen FlowIoC meets what it is before meeting how its folders are arranged.

- The agent rules spell out how a Model relates to signals, which was previously left to
  inference. A Model never subscribes to a signal: an incoming signal runs a Command and
  the Command calls the Model. A Model may dispatch its own module's outgoing signals to
  announce that a value it holds has changed — announcing is allowed, listening is not.
  The **Who does what** table in the README says the same thing.
- A Service is now defined by what it depends on rather than by what it talks to. The old
  wording made a Service the module's boundary with HTTP, storage and vendor SDKs, and
  forbade it from deciding anything. A Service is a self-contained unit of work that
  answers the input it is given, is not specific to the game around it, and is free to
  decide whatever its own job requires. A Service several modules need gets its own module.
- **System** joins the vocabulary: the game-specific counterpart to a Service. A System may
  lean on other Systems and Services, waiting on a signal they raise or working from data
  they share, which is exactly what a Service may not do. Both dispatch outgoing signals,
  and a Command drives either the same way it drives a Model. Systems live in `Systems/`,
  as an interface and an implementation.
- The rules name the exceptions to "a module never reaches into another module", which was
  stated absolutely while the framework has always had three. A Service crosses directly —
  reference its assembly, inject its interface, which is what makes a Service worth its own
  module. A nested module may use the types of the module it sits in, one way only: a
  module never knows what its own `z` folders hold. And a test module may reference
  anything, in exchange for every script in it being wrapped in `#if UNITY_EDITOR`.
  Systems are not on that list: two Systems in separate modules meet through a Connector,
  never through an assembly reference.
- `Create Module` produces a `Systems` folder in main modules. Screen and test modules are
  unchanged. The folder list is stored in your project's
  `MainModuleDirectoryStructureConfig.asset`, which an upgrade does not rewrite, so a
  project created before this release keeps its old list — add `Systems` in that asset's
  inspector, or delete the asset and let FlowIoC recreate it.
- Modules are no longer marked with text files. `_module_info.txt` held a module's name and
  kind, both already implied by the folder itself — its own name, and its parent's — and
  `_<foldertype>_info.txt` marked a container folder so a rename could be followed, which
  Unity's own folder GUIDs already do without help. Both are gone: a module's whole identity
  now lives in one `FlowIoCModuleIndex.asset`, next to the code generator settings, keyed on
  folder GUID rather than name or path. The index is a cache, not a source of truth — name,
  kind and nesting are read back off the folder tree on every rebuild, so a stale entry is
  repaired by rebuilding it, never by hand-editing it. Each module's folder-type GUID map is
  the exception: it is durable state, carried forward rather than read back off the tree and
  healed only lazily, so deleting the asset outright is not the same as rebuilding it. A
  project upgrading from an earlier version has its markers swept away the first time it opens
  after upgrading, once every pre-existing module's folder GUIDs are safely on record — 74
  files (37 markers and their `.meta` siblings) in this template's three modules.
- The Create Command window now lists modules. Its own copy of the marker filename was
  `module_info.txt`, missing FlowIoC's leading underscore, so its probe never matched a real
  `_module_info.txt` and the module tree came up empty. `Create Model` and `Create View` used
  the correct name and were never affected.
- A module's kind is decided in one place. `ModuleAutoDetector` compared the parent folder
  against the configured folder names, `DeleteModuleMenu` matched hardcoded `zTest` / `zScreen`
  / `zSub` prefixes and ignored the settings, and `NamespaceProvider` parsed a string out of a
  marker file and silently mapped anything it did not recognise to `Main`. Renaming
  `zScreenModules` in the settings used to make those three disagree with each other.
- Log types for deleted modules are removed. Registration only ever added a channel, so a
  module's channel and its constant in the generated `FlowLogType.cs` used to outlive the
  module's own folder. Removal only ever targets auto-registered channels, and only runs when
  the scan actually found at least one module, so a failed scan is never mistaken for a
  project with no modules and cannot wipe every channel.
- *Module Configuration ▸ Detect & Fix Module Infos* is now *Detect & Fix Module Index*. It has
  rebuilt the index rather than repaired info files since info files stopped existing, and the
  menu was the last place still saying otherwise.
- Folder types a module is not required to have no longer warn when they are missing. About half
  the tracked types are optional, so the warning that a folder could not be found fired several
  times per pass on a project that was perfectly healthy, which is exactly how a warning that
  matters gets ignored.

### Removed

- `SingletonRoot<TContext>`, its duplicate registry, and the *Make Root Singleton* option in
  *Create Module*. A Root that has to outlive a scene load now says so itself by overriding
  `BeforeCreateContext` and calling `DontDestroyOnLoad` there, which is all the base type did
  beyond standing duplicates down. **Breaking:** a project with `SingletonRoot` Roots must
  rebase them on `Root<TContext>` and add that override where the lifetime mattered. Nothing
  deduplicates Roots any more, so a Root that survives scene loads belongs in a bootstrap
  scene and nowhere else.

- `ModuleCleaner`, and with it *Module Configuration ▸ Cleanse Module Infos*. Its only job was
  pruning stale module names out of the container-folder marker files; rebuilding the index
  makes that pruning automatic, since a module missing from the folder tree is simply missing
  from the next rebuild.

### Fixed

- *Update Namespace Settings* survives a module the index is wrong about. The index is a cache,
  and `AssetDatabase.GUIDToAssetPath` answers for a deleted folder with the last path it knew
  rather than with nothing, so the path looked usable and the first `Directory.GetFiles` on it
  threw `DirectoryNotFoundException` - taking the whole run down, including the orphan cleanup
  and the solution code style, which have nothing to do with that module. A folder that is not
  on disk is now reported and stepped over the way an unresolvable folder GUID already was, a
  module that fails no longer stops the ones after it, and the two closing steps run whatever
  happened before them.

- The shipped code style declares `PD_` a legal type prefix. `CD_`, `RD_`, `ED_` and `DD_` were
  all there and the matching `PVO` suffix was too, so a player data asset was the one kind whose
  name Rider flagged.

- Reading a folder name for a type the project's `CodeGeneratorSettings.asset` has never
  heard of no longer throws `KeyNotFoundException`. That asset is written once in the
  consuming project, so any folder type added by a later FlowIoC version is missing from
  every existing copy, and indexing the dictionary directly took module creation down with
  it. Names now fall back to the built-in default.
- A rebuild that could not run no longer leaves its callers working from an empty index. The
  rebuild returned nothing and each caller loaded the index itself, which created an empty one
  and looked exactly like a project with no modules: a newly created module's folder GUIDs went
  unrecorded without a word, and log type detection would have proposed removing every
  auto-registered channel. The rebuild now hands back the index it built, or nothing at all.
- A recorded folder GUID that no longer resolves is looked for by name again. Deleting a folder
  or moving it outside Unity used to skip that folder type for good, because the name fallback
  only ran for a type that had never been recorded at all.
- Deleting the last module out of a renamed container folder now removes the emptied container.
  `zSubModules`, `zTestModules` and `zScreenModules` are configurable names, and the cleanup
  matched the hardcoded ones.

## [1.1.1] - 2026-08-23

### Fixed

- `FlowConsoleSettings.asset` is no longer replaced when it exists on disk but fails to load.
  Unity returns null both for an absent asset and for one whose script cannot be resolved —
  which happens while scripts are not compiling, or after the package's asset paths change —
  and FlowIoC treated the second case as the first, writing a fresh settings asset over the
  real one and taking every auto-registered module log type with it. The file on disk now
  gets the benefit of the doubt and a warning explains what to fix.
- `Assets/FlowIoC/Generated` is no longer deleted when the log type settings come back empty.
  A settings asset loaded from disk always carries its mandatory channels, so an object with
  none of them is a stand-in for one that failed to load, not a project with no log types.
  Deleting `FlowLogType.cs` on its word broke compilation, and the compile errors then kept
  the settings from loading — a loop that repeated on every domain reload.
- Declining the agent rules startup notice is now remembered per project. `EditorPrefs` is
  shared by every project opened with the same Editor, so "Do not ask again" used to silence
  the notice everywhere on the machine. `Tools/FlowIoC/AI/Agent Rules` also gained a button to
  switch the notice back on, which previously required the rules themselves to change.

## [1.1.0] - 2026-08-23

### Migrating from `com.flowioc.core`

Renaming a package changes the asset path of every script it ships. Unity keeps the
association between a script asset and its compiled type in `Library/`, and that
association does not survive the change: `MonoScript.GetClass()` starts returning null
for the package's scripts even though the types are compiled and loaded. Every
`ScriptableObject` the package defines then fails to load, and FlowIoC's own recovery
paths make that permanent — `FlowLogger` recreates `FlowConsoleSettings` from scratch,
losing the auto-registered module log types, and the log type generator, finding no log
types, deletes `Assets/FlowIoC/Generated`. `FlowLogType` disappears, every module that
logs stops compiling, and the compile errors keep the script association broken, so the
cycle repeats on each domain reload.

**Do the migration with the Editor closed:**

1. Close Unity.
2. Edit `Packages/manifest.json`: remove `com.flowioc.core`, add
   `"com.flowarc.flowioc.core": "https://github.com/FlowArc/FlowIoC.git#1.1.0"`.
3. Delete the project's `Library/` folder. It is derived and gitignored; Unity rebuilds
   it on the next open, which is what re-establishes the script associations.
4. Open the project. The first import takes a while.

If you renamed with the Editor open and hit the loop above, the recovery is the same
sequence: close Unity, restore `Assets/FlowIoC/Generated` and
`Assets/Resources/FlowConsoleSettings.asset` from version control **while Unity is
closed**, delete `Library/`, then reopen. Restoring them with the Editor running does not
hold — the running Editor rewrites them before the next compile finishes.

One smaller side effect: removing the old package strips the FlowIoC block from
`AGENTS.md`, because as far as the Package Manager is concerned FlowIoC was uninstalled.
Anything you wrote outside the block survives, and the new package offers to write the
rules back on the next domain reload.

### Added

- `Tools/FlowIoC/AI/Agent Rules` writes FlowIoC's architecture rules into the project's
  root `AGENTS.md` as a marked block and points `CLAUDE.md` at it, so AI coding
  assistants follow the framework's conventions instead of guessing at them. Only the
  text between the `FLOWIOC` markers is touched, so rules you wrote yourself survive, and
  a malformed marker makes the tool refuse to write rather than guess. FlowIoC offers to
  install the block on first open and to refresh it when the rules change — detected by
  hashing the rule text, so edits that ship without a version bump are still caught — and
  removes it again when the package is uninstalled through the Package Manager. The rule
  text ships in `Documentation~/AgentRules.md`.
- `[SignalParam]` accepts an index: `[SignalParam(1)]` binds to the second value of
  that property's type in the signal payload. The index counts within the type, so
  adding a parameter of another type to the signal does not shift it. Commands can
  now read a `Signal<int, int>` or a `Signal<string, string>` correctly.
- An EditMode test assembly at `Tests/Editor`, covering signal parameter resolution.
- A Scene Switcher dropdown on the main toolbar. It lists every scene under
  `Assets/Modules` as `ModuleName/SceneName` and opens the picked one, prompting to save
  the open scene in edit mode and loading through `SceneManager` in play mode. The main
  toolbar API arrived in Unity 6000.3, so the feature compiles out on earlier editors
  and the package minimum stays at 6000.0.

### Changed

- **Package name is now `com.flowarc.flowioc.core`** (was `com.flowioc.core`), matching the
  FlowArc organisation the repository lives under. The Package Manager treats a package
  name as its identity, so this is not an upgrade: remove `com.flowioc.core` from
  `Packages/manifest.json` and add `com.flowarc.flowioc.core` pointing at the new tag.
  Nothing else changes — assembly names, namespaces and the `FlowIoC` folder are untouched.
  **If you are upgrading an existing project, read the migration note below before you
  start.** A fresh install needs none of it.
- `[SignalParam]` properties are now discovered by walking the command's base-class
  chain directly rather than through the assembly scan the other injection attributes
  use. Two consequences for existing code: a `[SignalParam]` declared on a base class
  that lives in a *different assembly* from the command now binds, where it was
  previously skipped without a warning; and a `public` inherited `[SignalParam]` is
  now recorded once rather than twice, so it no longer consumes two payload values.
  Both shifts ripple: unindexed properties take the next unclaimed value of their
  type in declaration order, so a base-class property that newly joins the list — or
  a duplicate that stops joining it — changes which value the properties after it
  receive, and can leave the last one with none. `[Inject]` and `[InjectSignal]`
  discovery is unchanged.
- A dispatched `null` no longer logs `Signal Param is not found!`. It binds to any
  property whose type can hold it, which is the intended behaviour, but code that
  treated that message as a signal-shape alarm will stop seeing it.

### Fixed

- A command with two `[SignalParam]` properties of the same type received the same
  value in both. Properties without an index now take the next value of their type
  that no other property has claimed.
- Binding failures now name the command, the property and the reason instead of
  logging only the parameter type.

## [1.0.1] - 2026-08-19

### Fixed

- Guarded the `using UnityEditor;` directives in `ComponentReference.cs` and
  `ScreenConfig.cs` behind `#if UNITY_EDITOR`. Both files belong to the runtime
  assembly, which is compiled for players, so the unguarded directives broke player
  builds in consuming projects. Verified by compiling the player script assemblies.

### Changed

- **Breaking:** renamed the namespace `FlowIoC.PoolModule.Addressable.Components` to
  `FlowIoC.PoolModule.Components`. `ComponentReference` and
  `AssetReferenceSpawnableObject` moved with it. Update any `using` directive that
  referenced the old namespace.
- Moved `ScreenManager.prefab` out of the package's `Resources` folder to
  `Assets/Prefabs/`, alongside the other code generator templates. Nothing ever loaded
  it through `Resources.Load`; the generator resolves it with `AssetDatabase`, so the
  `Resources` folder only forced the asset into every consumer build. The asset GUID is
  unchanged, so prefab links in already generated scenes still resolve. The package no
  longer ships a `Resources` folder at all.

## [1.0.0] - 2026-08-19

First tagged release, installable through the Unity Package Manager.

### Added

- `CHANGELOG.md` and package manifest metadata (`license`, `documentationUrl`,
  `changelogUrl`, `licensesUrl`, keywords, author) so the package presents itself
  properly in the Package Manager window.
- Declared runtime dependencies on `com.unity.addressables` and
  `com.unity.render-pipelines.core`. These were used by the code but never declared,
  so installing the package into a project without them failed to compile.

### Changed

- Package name is now `com.flowioc.core` (was `flow-ioc`), following the reverse
  domain naming that the Package Manager and scoped registries require.
- Minimum supported editor is now Unity 6000.0 (the manifest previously claimed 2019.1).
- The code generator resolves the package root from its own assembly instead of
  assuming the package sits at `Packages/FlowIoC`. Template and prefab lookups now
  work for Git URL and registry installs, not just embedded ones.

### Removed

- Dead assembly reference to `com.unity.editorcoroutines`; nothing in the package used it.
- Leftover `Packages/manifest.json` and `Packages/packages-lock.json` from when this
  repository was a standalone Unity project.
