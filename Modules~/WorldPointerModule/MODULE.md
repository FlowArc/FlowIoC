# WorldPointer

## Purpose
Joins what stands in the world to the screen that draws it. Both sides meet on a channel - a name
such as `Emote` that says what kind of pointer it is. The module that owns a world object registers
its Transform on a channel and sends requests - content, show, hide; a screen registers as that
channel's display. While both exist the service moves the display's indicator to the object's
screen position every frame and decides what happens when it leaves the frame - hide, clamp to the
edge with an arrow, or ignore.

## Concepts
world pointer, off-screen indicator, world to screen, HUD marker, health bar over a unit, emote over
a head, speech bubble, edge clamp, arrow, channel, target, display, IWorldPointerService,
IWorldPointerDisplay, WorldPointerPoolDisplay, WorldPointerIndicator, RD_WorldPointer

## Decisions
- **The world side never reaches a canvas.** An overlay canvas exists only through the
  ScreenManager, so the UI of a pointer belongs to a screen: the module that owns the targets
  owns a screen module in its `zScreenModules` that registers a display for the channel. The
  world side sends requests and never learns whether they were applied.
- **A channel is what a pointer is, the Transform is which one.** A screen draws a channel, so it
  never has to know the world objects; the world side names the channel and the object. The pair
  is the target's key, and no handle is kept anywhere, so a stateless Command that holds the
  Transform can send a request or unregister. Two pointers on one object use two channels.
- **Every lookup is one step.** The Model keys targets by channel and Transform - the Transform by
  reference, so a destroyed one still finds its entry - and each channel keeps its members in a
  list a target leaves by swapping with the last. A register, a request or an unregister costs the
  same with a thousand targets as with one, and a frame walks only the channels that have a display.
- **Nothing is returned.** A refused call - a pair already registered, a request for a pair that is
  not, a second display for a channel - is an error naming the file and line that made it, and is
  ignored. There is no bool for a caller to branch on.
- **One display per channel**, and a second is refused. What a target was told while no display was
  up - its last content and its last Show or Hide - is kept and replayed to the next display.
  `UnregisterDisplay` takes the channel, so the screen's closing Command needs nothing it kept.
- **Indicators come from the FlowIoC pool.** `WorldPointerPoolDisplay` is the ready display: the
  screen's register Command builds it with the pool, an item key and a RectTransform its View holds.
  The indicator prefab is a pool item like any other, and `WorldPointerIndicator` is a
  `PoolableItem`: the pool reparents keeping world values, so it puts the prefab's own scale and
  rotation back when it comes out. An indicator that fades out on the way overrides `Dismiss` and
  calls the base at the end. Acquire is the pool's synchronous Get, so an addressable indicator has
  to be in a warmed group.
- **The display owns the indicators and the options.** It hands them out and takes them back;
  the service never tells an indicator Hidden on the way out. Offsets, margins, clamp and arrow are
  the display's, because the screen knows its layout.
- **The service is a front.** The registry is `WorldPointerModel`'s, one target's frame is
  `WorldPointerStepper`'s, and the frame and its margins are `WorldPointerFrame`'s; the service
  checks calls, matches targets with displays and runs the one LateUpdate.
- **One step shipped, `IWorldPointerService.Commands.UnregisterAll`.** Registering takes a
  Transform and a display that exist at runtime, so it stays a call from the game's own Command;
  clearing every target is a fixed step of the flow that leaves the scene.
- **`RD_WorldPointer` is for the Inspector.** One row per channel - the display, every target, its
  last content and state. The rows are the Model's own objects, written in place and emptied at
  boot; nothing is rebuilt, and nothing reads the serialized fields back.
- A pointer is placed by world point on its parent's plane rather than by `anchoredPosition`,
  which is what makes any parent and any anchors correct in both canvas modes. The clamp ray
  starts at the frame's own centre, so uneven margins need no special case. Behind the camera
  the projection is mirrored back through the centre before anything reads it. The arrow turns by
  local rotation, which is right in both canvas modes where a world-space up is not.
- **A clamped indicator stays whole on screen.** In `ClampToEdge` the frame is shrunk by the
  indicator's own half size in screen pixels, read from its corners, so the edge is where all of
  it still shows - a margin in pixels could not do that at every resolution.
- **The sample screens are Editor-only.** `PointerSampleScreenModule` is the test module's
  display and `PointerControlsScreenModule` its buttons, so their scripts sit under
  `#if UNITY_EDITOR`, they load from Resources, and their prefabs live in `Editor/` folders -
  nothing of them reaches a player build or an Addressables group. The test scene has no canvas
  of its own; everything it shows opens through the ScreenManager. Its `PoolServiceRoot` files
  `CD_PoolGroup_PointerSample`, the group the sample's labels come from.

## Known gaps
- One camera. Split screen and render-texture cameras are not handled.
- `Ignore` keeps placing a pointer whose target is behind the camera, at the mirrored point.
- An indicator destroyed under the service is let go and its target waits until the display
  registers again.
- A target destroyed while its channel has no display stays in the registry until a display
  registers or the target is unregistered; a frame does not walk undrawn channels.
- A screen destroyed without unregistering its `WorldPointerPoolDisplay` takes the indicators
  under it with it, and the pool keeps them counted as active.

Version: 1.2.0

<!-- FLOWIOC:BEGIN version=1 hash=44a58d1e | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.WorldPointer
**Root** WorldPointerServiceRoot → WorldPointerServiceContext
**Services** IWorldPointerDisplay, IWorldPointerDisplay`1, IWorldPointerIndicator, IWorldPointerIndicator`1, IWorldPointerService
**Sub modules** PointerControlsScreenModule (Screen) · PointerSampleScreenModule (Screen) · WorldPointerTestModule (Test)
<!-- FLOWIOC:END -->
