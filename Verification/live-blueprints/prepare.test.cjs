// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } = require('node:fs');
const { join, relative, resolve } = require('node:path');
const { test } = require('node:test');

const entry = require('./blueprints.json')[0];
const manifest = {
    name: 'CRM fixture',
    version: '1.2.3',
    blueprint: { templates: ['customers'] },
    sampleData: { customers: [{ name: 'Sample customer' }] },
};

function fixture(context, esm) {
    const work = resolve(__dirname, '../../.ai-work');
    mkdirSync(work, { recursive: true });
    const root = mkdtempSync(join(work, 'prepare test-'));
    context.after(() => rmSync(root, { recursive: true, force: true }));
    const modules = join(root, 'node_modules');
    const folder = join(modules, '@cratis', entry.npm);
    mkdirSync(join(folder, 'dist'), { recursive: true });
    writeFileSync(join(folder, 'package.json'), JSON.stringify({
        name: `@cratis/${entry.npm}`,
        version: '4.5.6',
        main: './dist/index.cjs',
        exports: { '.': { import: './dist/index.mjs', require: './dist/index.cjs' } },
    }));
    writeFileSync(join(folder, 'dist/index.cjs'), 'throw new Error("CJS must not be loaded");');
    writeFileSync(join(folder, 'dist/index.mjs'), esm);
    writeFileSync(join(folder, 'dist/browser.js'), '// browser fixture\n');
    const out = join(root, 'packages');
    const result = spawnSync(process.execPath, [join(__dirname, 'prepare.cjs'), relative(process.cwd(), modules), '0', out], {
        encoding: 'utf8',
        timeout: 10000,
    });
    assert.ifError(result.error);
    return { out, result };
}

test('loads the ESM manifest intact and copies the published browser build', context => {
    const { out, result } = fixture(context, `export const ${entry.export}Manifest = ${JSON.stringify(manifest)};`);

    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(JSON.parse(readFileSync(join(out, 'approved-packages.json'), 'utf8')), {
        version: 1,
        packages: [{
            manifest,
            module: { npm: `@cratis/${entry.npm}`, version: '4.5.6', export: entry.export, styles: [] },
        }],
    });
    assert.equal(readFileSync(join(out, '@cratis', entry.npm, 'dist/browser.js'), 'utf8'), '// browser fixture\n');
    assert.equal(JSON.parse(readFileSync(join(out, '@cratis', entry.npm, 'package.json'), 'utf8')).version, '4.5.6');
});

test('fails nonzero when the ESM entry does not export the expected manifest', context => {
    const { out, result } = fixture(context, 'export const unrelated = {};');

    assert.equal(result.status, 1, result.stderr);
    assert.ok(result.stderr.includes(`does not export ${entry.export}Manifest`), result.stderr);
    assert.equal(existsSync(join(out, 'approved-packages.json')), false);
});
