# Icon sources

The drawings FlowIoC's windows use, one SVG per `FlowIcon` value. They are FlowIoC's own: every
one is drawn on the same 16 by 16 grid, white on transparent, as 1.5 unit round-capped strokes,
with a filled shape only where a silhouette reads better at 16 pixels than an outline does. The
folder name ends in `~` so Unity never imports the SVGs; what the Editor draws is the PNGs beside
this folder.

## Adding an icon

1. Draw `Name.svg` here, on the same grid and in the same stroke as the others. Keep to whole and
   half units so the strokes land on pixels at 16.
2. Rasterise it at every size the set ships, into the folder above:

   ```
   npm install @resvg/resvg-js
   node render.js .. 16 24 32 48
   ```

   `render.js` reads every SVG in this folder and writes `Name-16.png` and the others beside it.
   Nothing else rasterises them: an icon scaled by the Editor at draw time is exactly the blur
   the set replaces.
3. Add `Name` to `FlowIcon` with the next free number. A number a deleted icon used is never
   given to another.

The import settings a new PNG needs - a GUI texture, uncompressed, no mip chain - are applied by
`FlowIconImporter` on import, and `FlowIconsTests` in the package's own tests fails until every
value ships every size and every file in the folder belongs to a value.

## Sizes

`16` and `24` are the two point sizes the windows draw an icon at; `32` and `48` are the same two
at 2x for a high-DPI screen. A draw at a scale no size matches takes the next size up, scaled
down, which is what every other picture in the Editor gets at that scale.
