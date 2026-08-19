# Dependency Diagram

**Tools ▸ FlowIoC ▸ Dependency Diagram**

The Dependency Diagram reads your contexts and draws the graph they actually form:
which signals exist, which commands each one triggers, in what order, which views are
mediated, and where two modules touch.

It answers questions that are tedious to answer by reading code — *what happens when
this signal fires*, *who listens to this module*, *is anything still connected to the
screen I deleted* — and it answers them from the current source, not from a diagram
somebody drew once.

- [When to Open It](#when-to-open-it)
- [Views](#views)
- [Navigating](#navigating)
- [Filtering and Search](#filtering-and-search)
- [Simulation](#simulation)
- [Scenarios](#scenarios)
- [Pitfalls](#pitfalls)

---

## When to Open It

| Situation | What the diagram gives you |
|---|---|
| Joining a codebase | The real module map, in one screen |
| Before deleting a module | Every edge still pointing at it |
| A signal does nothing | Whether anything is bound to it at all |
| Explaining a flow to someone | An animated path instead of a walkthrough of six files |
| Reviewing coupling | Which modules touch, and through which connector |

---

## Views

Each view is the same data drawn for a different question.

| View | Answers |
|---|---|
| **CategoryView** | *What exists?* Nodes grouped by type — contexts, signals, commands, views, mediators, models, services |
| **ContextDiagram** | *How do contexts relate?* One node per context and the dependencies between them |
| **SignalFlow** | *Where does this signal go?* Signals and everything they reach |
| **CommandFlow** | *What runs, in what order?* Command chains, with sequence and parallel groups drawn as groups |
| **FlowView** | *What is the whole path?* Sequential and parallel flow end to end |
| **CompactView** | *What is the shape of the system?* Minimal overview for large projects |

Start in CompactView or ContextDiagram to orient yourself, then switch to SignalFlow
or CommandFlow once you know which corner you care about.

---

## Navigating

| Action | Control |
|---|---|
| Pan | Middle-mouse drag, or Alt + left-mouse drag |
| Zoom | Mouse wheel |
| Select | Click a node or a group |
| Frame everything | *Reset View* |

Selecting a node fills the detail panel. For a command it shows the execution type,
its position in the chain and what it depends on; for a signal it lists every command
that signal triggers. That panel is usually faster than opening the context file,
because it already resolved the chain across sub-groups.

Command groups can be collapsed. On a context with thirty bindings, collapsing the
chains you are not looking at is the difference between a readable diagram and a
hairball.

Export to PNG when you want the picture in a design document or a pull request.

---

## Filtering and Search

The filter bar toggles node types: hide views and mediators while tracing data flow,
hide models while tracing UI. Search matches node names and highlights the hits, which
is how you find a specific command in a large context without scanning.

Filters and search compose — filter to commands, search for `Purchase`, and you have
every purchase-related command in the project with its chains intact.

---

## Simulation

The diagram can play a signal through the graph, one step at a time: dispatch the
signal, watch the nodes activate in order, step forward and back.

Use it for two things:

- **Verifying a chain you just wrote** without entering play mode.
- **Explaining a flow.** An animated path through the real graph is a better
  explanation than a walkthrough of six files, and it cannot go stale.

The simulation shows structure, not behaviour: it follows the bindings, so it walks
every branch a chain declares. It does not know which branch a command would actually
take at runtime — for that, watch the same flow in the
[Flow Console](../../Runtime/ConsoleModule/Documentation/FlowConsole.md) while
playing.

---

## Scenarios

### Check the graph before deleting

```
✅ Open ContextDiagram, select the module, read its incoming edges. Every connector
   that still points at it shows up as a line you have to remove first.
```

```
❌ Delete the module, compile, and work through the errors. Connector wiring fails at
   runtime rather than at compile time, so the errors do not tell you the whole story.
```

### Read the diagram when a signal does nothing

```
✅ SignalFlow, search for the signal. A node with no outgoing edge means nothing is
   bound to it — the problem is the binding, not the dispatch.
```

```
❌ Add logs to every command that might be involved, and to the mediator, and to the
   connector, and re-enter play mode four times.
```

### Filter before you read

```
✅ Trace data flow with views and mediators hidden. Half the nodes disappear and the
   path becomes visible.
```

```
❌ Read the unfiltered CategoryView of a thirty-module project and conclude the tool
   is not useful.
```

---

## Pitfalls

### A node you expect is missing

The analyzer reads binding calls in context classes. A binding built indirectly — in
a loop, behind a helper method, or from a collection — is not something it can see.
Bindings written plainly, one call per line, are what makes both the diagram and the
context file readable.

### The diagram is stale

It reflects the source at the moment it was analysed. Re-run the analysis after
editing a context; the diagram does not watch the filesystem.

### It disagrees with what happens at runtime

The diagram shows what is *bound*, not what *ran*. A command that returned early, a
retained command that never released, or a connection torn down with
`DisconnectGroup` all leave the diagram unchanged. Use the Flow Console for what
actually happened, and the diagram for what could happen.

---

## Related

- [Editor Tools](../README.md)
- [Flow Console](../../Runtime/ConsoleModule/Documentation/FlowConsole.md) — runtime
  counterpart to this static view
- [Commands](../../Runtime/BaseModule/Controller/Documentation/Controller.md) — the
  chains this diagram draws
