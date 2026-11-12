// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExtensionComponentDefinition, ExtensionComponentKind } from '@cratis/scene.extensions';

/** Loads a module by address. The browser's `import()` by default; a spec supplies its own. */
export type ExtensionModuleImporter = (address: string) => Promise<Record<string, unknown>>;

const browserImporter: ExtensionModuleImporter = address => import(/* @vite-ignore */ address) as Promise<Record<string, unknown>>;

const loaded = new Map<string, Promise<Record<string, unknown>>>();

/**
 * Loads an extension's frontend module once per address. Addresses carry the digest of the locked package, so a module never changes
 * under its address and a cached load is never stale.
 */
export function loadExtensionModule(address: string, importer: ExtensionModuleImporter = browserImporter): Promise<Record<string, unknown>> {
    const cached = loaded.get(address);
    if (cached) return cached;
    const loading = importer(address).catch(error => {
        loaded.delete(address); // A failed load is asked for again the next time, not remembered.
        throw error;
    });
    loaded.set(address, loading);
    return loading;
}

/** Forgets every loaded module. For specs. */
export function forgetExtensionModules(): void {
    loaded.clear();
}

/** Where a component is found and what the screen placed it as. */
export interface ExtensionDefinitionRequest {
    /** The address of the extension's frontend module, which the host serves from the locked package. */
    module: string;

    /** The name the module exports the component under, which is its name in the contract. */
    component: string;
    kind: ExtensionComponentKind;
    importer?: ExtensionModuleImporter;
}

const isDefinition = (value: unknown): value is ExtensionComponentDefinition =>
    typeof value === 'object' && value !== null && 'kind' in value && ((value as { kind: unknown }).kind === 'react' || (value as { kind: unknown }).kind === 'mount');

/**
 * Finds the component's definition in the module and checks it is the kind the model says it is. A module that exports something
 * else, or a component of the other kind, is refused with a message that names which of the two is wrong.
 */
export async function loadExtensionDefinition(request: ExtensionDefinitionRequest): Promise<ExtensionComponentDefinition> {
    const module = await loadExtensionModule(request.module, request.importer);
    const definition = module[request.component] ?? (module.default as Record<string, unknown> | undefined)?.[request.component];
    if (!isDefinition(definition)) {
        throw new Error(`The frontend of the extension does not export a component named ${request.component}; export it with reactComponent or mountComponent.`);
    }
    if (definition.kind !== request.kind) {
        throw new Error(`${request.component} is placed as a ${request.kind} component, but the extension exports it as a ${definition.kind} component.`);
    }
    return definition;
}
