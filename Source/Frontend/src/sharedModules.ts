// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from 'react';
import * as JsxDevRuntime from 'react/jsx-dev-runtime';
import * as JsxRuntime from 'react/jsx-runtime';
import * as ReactDom from 'react-dom';
import * as ReactDomClient from 'react-dom/client';

/** The browser global the shim modules of the import map read the Stage's modules from. Mirrors `stageSharedModules.ts`. */
export const sharedModulesGlobal = '__cratisStageShared';

/** The modules the Stage shares with approved custom packages, by the specifier a package imports them by. */
export const sharedModules: Record<string, unknown> = {
    'react': React,
    'react/jsx-runtime': JsxRuntime,
    'react/jsx-dev-runtime': JsxDevRuntime,
    'react-dom': ReactDom,
    'react-dom/client': ReactDomClient,
};

/**
 * Registers the Stage's own React for the import map's shim modules, so a custom package loaded at runtime renders
 * with the same React the Stage does. Must run before any package is imported.
 * @param target Where to register; the page's global by default.
 */
export function registerSharedModules(target: Record<string, unknown> = globalThis as unknown as Record<string, unknown>): void {
    target[sharedModulesGlobal] = sharedModules;
}
