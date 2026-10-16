// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Builds the approved package set of one published blueprint exactly as it is written - the manifest keeps its `blueprint` and
// `sampleData` members, which the approved set must accept and drop (Cratis/Stage#457) - and copies the package's browser build into
// the folder the Stage serves packages from.
//
// usage: node prepare.cjs <node_modules directory> <blueprints.json entry index> <output directory>
const { cpSync, mkdirSync, writeFileSync } = require('node:fs');
const { join } = require('node:path');

const [modules, index, out] = process.argv.slice(2);
const entry = require('./blueprints.json')[Number(index)];
const published = require(join(modules, '@cratis', entry.npm));
const manifest = published[`${entry.export}Manifest`];
if (!manifest) throw new Error(`@cratis/${entry.npm} does not export ${entry.export}Manifest`);

const folder = join(out, '@cratis', entry.npm);
mkdirSync(folder, { recursive: true });
cpSync(join(modules, '@cratis', entry.npm), folder, { recursive: true, dereference: true });
writeFileSync(join(out, 'approved-packages.json'), `${JSON.stringify({
    version: 1,
    packages: [{ manifest, module: { npm: `@cratis/${entry.npm}`, version: require(join(modules, '@cratis', entry.npm, 'package.json')).version, export: entry.export, styles: [] } }],
}, null, 2)}\n`);
console.log(`${manifest.name} ${manifest.version} <- @cratis/${entry.npm}`);
