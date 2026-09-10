// Rasterises every SVG beside this file at the sizes asked for, into the folder given:
//
//   node render.js <outDir> <size> [<size> ...]
//
// Each SVG becomes one PNG per size, named Name-<size>.png. The rasteriser is resvg, installed
// with `npm install @resvg/resvg-js` next to this file; see README.md.
const fs = require('fs');
const path = require('path');
const { Resvg } = require('@resvg/resvg-js');

const [outDir, ...sizes] = process.argv.slice(2);

if (!outDir || sizes.length === 0) {
  console.error('usage: node render.js <outDir> <size> [<size> ...]');
  process.exit(1);
}

fs.mkdirSync(outDir, { recursive: true });

let written = 0;

for (const file of fs.readdirSync(__dirname).filter(f => f.endsWith('.svg')).sort()) {
  const svg = fs.readFileSync(path.join(__dirname, file), 'utf8');
  const name = path.basename(file, '.svg');

  for (const size of sizes.map(Number)) {
    const png = new Resvg(svg, { fitTo: { mode: 'width', value: size }, background: 'rgba(0,0,0,0)' })
      .render()
      .asPng();

    fs.writeFileSync(path.join(outDir, `${name}-${size}.png`), png);
    written++;
  }
}

console.log(`${written} pngs`);
