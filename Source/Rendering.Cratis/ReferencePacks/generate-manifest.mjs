// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createHash } from 'node:crypto';
import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

const [packageDirectory, version, framework] = process.argv.slice(2);
if (!packageDirectory || !version || !framework) {
    throw new Error('Usage: node generate-manifest.mjs <package-directory> <version> <framework>');
}
const directory = join(packageDirectory, 'ref', framework);
const files = (await readdir(directory)).filter(name => name.endsWith('.dll')).sort();
if (files.length === 0) {
    throw new Error(`No reference assemblies found in ${directory}`);
}
const lines = await Promise.all(files.map(async name => {
    const hash = createHash('sha256').update(await readFile(join(directory, name))).digest('hex');
    return `${hash}  ${name}\n`;
}));
await writeFile(new URL(`${version}-${framework}.sha256`, import.meta.url), lines.join(''));
