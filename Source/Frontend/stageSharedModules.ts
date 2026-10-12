// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createRequire } from 'node:module';
import type { Plugin } from 'vite';

/** The browser global the Stage registers the modules it shares with approved custom packages on. */
export const sharedModulesGlobal = '__cratisStageShared';

/** The modules a custom package built for a live Stage imports by bare name instead of bundling its own copy. */
export const sharedModuleSpecifiers = ['react', 'react/jsx-runtime', 'react/jsx-dev-runtime', 'react-dom', 'react-dom/client'] as const;

/** Where a shared module's browser entry is served, relative to the page - what the import map in `index.html` points at. */
export const sharedModuleFile = (specifier: string) => `stage-shared/${specifier.replace(/\//g, '--')}.js`;

const identifier = /^[A-Za-z_$][\w$]*$/;
const reserved = new Set(['default', 'arguments', 'eval']);

/**
 * The source of the module an import map maps a shared specifier to.
 *
 * It re-exports the module instance the Stage itself was built with, taken from the Stage's registration on
 * {@link sharedModulesGlobal}, so a package's hooks and contexts and the Stage's are the same ones. A package that
 * bundled its own React would have two copies and fail on its first hook; this is what the import map prevents.
 * @param specifier The module the package imports.
 * @param names The names the module exports.
 * @returns The source of the shim module.
 */
export function sharedModuleSource(specifier: string, names: string[]): string {
    const exported = names.filter(name => identifier.test(name) && !reserved.has(name)).sort();
    return [
        `const shared = globalThis.${sharedModulesGlobal}?.[${JSON.stringify(specifier)}];`,
        `if (!shared) throw new Error(${JSON.stringify(`The Stage has not registered ${specifier} for custom packages.`)});`,
        'const module = shared.default ?? shared;',
        'export default module;',
        ...exported.map(name => `export const ${name} = module.${name};`),
        '',
    ].join('\n');
}

/**
 * Emits the shim module for every shared specifier, so the import map of a live Stage can give approved custom
 * packages the Stage's own copy of React.
 * @returns The Vite plugin.
 */
export function stageSharedModules(): Plugin {
    const require = createRequire(import.meta.url);
    return {
        name: 'stage-shared-modules',
        generateBundle() {
            for (const specifier of sharedModuleSpecifiers) {
                this.emitFile({
                    type: 'asset',
                    fileName: sharedModuleFile(specifier),
                    source: sharedModuleSource(specifier, Object.keys(require(specifier) as object)),
                });
            }
        },
    };
}
