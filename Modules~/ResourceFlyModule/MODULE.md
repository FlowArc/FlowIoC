# ResourceFly

## Purpose
Flies a resource into its counter - coins, gems, stars - and keeps what the counter shows apart
from what is saved while it flies: shown = saved - pending, per resource key. Icons come from the
pool, leave a named source and play a motion into a named counter.

## Concepts
resource fly, coin fly, currency fly, reward animation, fly animation, spend fly, fly to counter,
count up, punch, flash, tint, landing, pending, reserve, shown value, saved value, source,
counter, motion, scatter, direct, curved, IResourceFlyService, IResourceFlyCounter,
ResourceFlyCounterDisplay, ResourceFlyIcon, CD_ResourceFly, CD_ResourceFlyMotion,
CD_ResourceFlyLanding, flight look, named look, look, visual only, inventory, inventory box, sprite,
banknote, units per icon, max icons, ResourceFlyLookVO

## Decisions
- **shown = saved - pending, and only pending is kept.** The saved value is only ever as last
  told and the shown one is worked out from it, so a lost icon cannot leave the counter wrong.
- **Reserve before the save.** A grant is reserved ahead of the save that announces it - the
  sequence starts with `IResourceFlyService.Commands.Reserve` - so the announcement leaves the
  counter where it was. A quit loses nothing: the save was written first.
- **A flight takes only what is not already flying.** A second Fly while the first is out flies
  what is left; asked for more than is pending, it flies what there is and warns.
- **A flight chooses its own look.** A `ResourceFlyLookVO` - pool key, sprite, units per icon,
  max icons, motion, landing - comes from code or as a named look in `CD_ResourceFly`'s Looks that
  the route names. Field by field the given look comes first, then the named look, the counter,
  and `CD_ResourceFly`. A sprite is laid on the icon's Image, so one pool item serves every
  picture - a weapon's own icon - and a pool key picks another prefab - a banknote.
- **Counting is the flight's choice, not the counter's.** A `VisualOnly` look flies the amount
  without a Reserve and changes no value; its landings and events still play. A counter whose
  Count is empty - an inventory box - shows no number and still takes either kind of flight.
- **A flight that cannot play settles at once.** No counter, no source, no motion, no pool item, an
  icon lost on the way - its screen hidden or destroyed under it: the share stops being pending,
  the counter shows the saved value, and the Fly step is released. A counter destroyed with its
  screen counts as unregistered, and a flight's end goes to the counter it began on.
- **Unregistering names the counter.** `UnregisterCounter(key, counter)` lets the key go only if
  that counter still holds it, so a popup closing over the top bar leaves the top bar registered.
- **Sources and counters register by name.** No screen position travels in a signal; the route is
  fixed where the Fly step is bound, the amount comes on the signal.
- **The motion is data the icon plays.** A `CD_ResourceFlyMotion` only answers where an icon is at
  a moment; the icon plays it and alone reports landed or lost, so a game's own motion cannot
  leave a flight unfinished. Scatter, Direct and Curved ship; a counter may name its own, else
  CD_ResourceFly's default plays. A motion that owns its playback (a tween library, an Animator)
  is not offered yet.
- **Size is part of the motion.** Every motion carries one ScaleCurve over the whole flight, flat
  at 1 by default, so a scale needs no tick and a game's own motion gets it too. A curve per
  phase (Scatter's burst and gather) is not offered: the split moves with the seconds, and one
  curve has been enough so far.
- **The counter's answer to a landing is data too.** A `CD_ResourceFlyLanding` only answers how
  the counter's icon looks at a moment - a scale and a tint; the counter plays it and puts the
  icon back to its own look when it ends, so a game's own landing cannot leave the icon changed.
  Punch (grows), Flash (runs through a gradient) and Tint (takes a colour, fades back) ship; a
  counter may name its own, else CD_ResourceFly's default plays. A tint needs the counter's
  Tinted slot, the icon's Image, and keeps the icon's own alpha.
- **The icons and the counter animate themselves on their own Update**, on unscaled time and with
  no tween library. It is the look of the flight and decides nothing, so it is not a tick
  sequence; every decision stays in the service and the Fly step.
- **It plays nothing.** The counter raises FlightStarted, Landed and FlightEnded and the hosting
  screen binds the haptic, the sound and any particle. A particle is not shipped: a ParticleSystem
  does not draw on a Screen Space Overlay canvas, so how it is drawn is the game's choice.
  FlightEnded waits for the count and the landing to rest,
  and a flight starting meanwhile continues the same stretch with no new FlightStarted. A listener
  that throws on FlightStarted is logged and the icons land anyway.
- **The sample screen lives in the test module.** `ResourceFlySampleScreenModule` sits under
  `ResourceFlyTestModule/zScreenModules`, editor-only with its assets under its `Editor/` folder,
  and carries no card of its own. Its three lanes show the three motions and the three landings:
  Scatter with the default Punch, Direct with Flash, Curved with Tint. The Direct lane also flies
  100 as banknotes through a named look, and a Box lane below - a counter with no Count - takes
  visual-only flights: a hat or a dress with its own sprite, 25 stones as ten icons. The scene files
  `CD_ResourceFly_Test`, a copy holding those looks, so the shipped `CD_ResourceFly` stays empty.

## Known gaps
- Only grants fly. A spend lowers the saved value and the counter drops at once.

Version: 1.2.0

<!-- FLOWIOC:BEGIN version=1 hash=fd9ca250 | generated by Tools/FlowIoC/Module Scanner - do not edit inside this block -->
**Kind** Main · **Assemblies** Modules.ResourceFly
**Root** ResourceFlyServiceRoot → ResourceFlyServiceContext
**Services** IResourceFlyCounter, IResourceFlyService
**Sub modules** ResourceFlyTestModule (Test)
<!-- FLOWIOC:END -->
