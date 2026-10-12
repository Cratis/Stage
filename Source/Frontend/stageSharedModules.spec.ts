// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createRequire } from 'node:module';
import { describe, expect, it } from 'vitest';
import { sharedModuleFile, sharedModuleSource, sharedModuleSpecifiers, sharedModulesGlobal } from './stageSharedModules';

describe('the module a live Stage maps a shared specifier to', () => {
    it('re-exports the instance the Stage registered, not a copy', () => {
        const source = sharedModuleSource('react', ['useState', 'createElement']);
        expect(source).toContain(`globalThis.${sharedModulesGlobal}?.["react"]`);
        expect(source).toContain('export const createElement = module.createElement;');
        expect(source).toContain('export const useState = module.useState;');
        expect(source).toContain('export default module;');
    });

    it('leaves out a name that is not a valid export', () => {
        const source = sharedModuleSource('react', ['default', 'not-valid', 'useState']);
        expect(source).not.toContain('not-valid');
        expect(source).not.toContain('export const default');
    });

    it('stops with a reason when the Stage has not registered the module', () => {
        expect(sharedModuleSource('react', [])).toContain('The Stage has not registered react for custom packages.');
    });

    it('gives each shared specifier a file the import map can point at', () => {
        expect(sharedModuleSpecifiers.map(sharedModuleFile)).toEqual([
            'stage-shared/react.js',
            'stage-shared/react--jsx-runtime.js',
            'stage-shared/react--jsx-dev-runtime.js',
            'stage-shared/react-dom.js',
            'stage-shared/react-dom--client.js',
        ]);
    });

    it('exports every name React and its JSX runtime export', () => {
        const require = createRequire(import.meta.url);
        const source = sharedModuleSource('react/jsx-runtime', Object.keys(require('react/jsx-runtime') as object));
        for (const name of ['jsx', 'jsxs', 'Fragment']) expect(source).toContain(`export const ${name} = module.${name};`);
    });
});

describe('the import map of the Stage page', () => {
    it('maps every shared specifier to the file the build emits for it', async () => {
        const { readFile } = await import('node:fs/promises');
        const html = await readFile('index.html', 'utf8');
        const map = JSON.parse(/<script type="importmap">([\s\S]*?)<\/script>/.exec(html)![1]) as { imports: Record<string, string> };
        expect(map.imports).toEqual(Object.fromEntries(sharedModuleSpecifiers.map(specifier => [specifier, `./${sharedModuleFile(specifier)}`])));
    });
});
