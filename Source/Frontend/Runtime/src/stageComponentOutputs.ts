// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useId, useRef } from 'react';
import { useStageData } from './stageData';

/**
 * The `onComponentOutputsChanged` of one rendered tree - the screen, or a dialog above it.
 *
 * Scene keeps the outputs of each tree it renders apart and hands over all of them on every change, so a tree publishes under a scope of its own:
 * a dialog that opens publishes beside the screen instead of over it, and when it closes only what it published goes - the row the screen had
 * selected still resolves.
 *
 * @returns The callback to hand the tree's `SceneElementView`.
 */
export function useComponentOutputsPublisher(): ((outputs: Record<string, Record<string, unknown>>) => void) & { readonly scope: string } {
    const scope = useId();
    const { publishComponentOutputs, withdrawComponentOutputs, selectRow, clearSelection } = useStageData();
    const selections = useRef<Record<string, Record<string, unknown>>>({});
    useEffect(() => () => {
        withdrawComponentOutputs(scope);
        Object.keys(selections.current).forEach(component => clearSelection(`${scope}:${component}`));
    }, [clearSelection, scope, withdrawComponentOutputs]);
    const publish = useCallback((outputs: Record<string, Record<string, unknown>>) => {
        publishComponentOutputs(scope, outputs);
        const next = selectedItemsOf(outputs);
        const components = new Set([...Object.keys(selections.current), ...Object.keys(next)]);
        for (const component of components) {
            if (selections.current[component] !== next[component]) selectRow(`${scope}:${component}`, next[component]);
        }
        selections.current = next;
    }, [publishComponentOutputs, scope, selectRow]);
    return Object.assign(publish, { scope });
}

/** Native selectedItem outputs join the same selection state that Stage tables already publish. */
function selectedItemsOf(outputs: Record<string, Record<string, unknown>>): Record<string, Record<string, unknown>> {
    const selections: Record<string, Record<string, unknown>> = {};
    for (const [component, output] of Object.entries(outputs)) {
        const row = output.selectedItem;
        if (isRecord(row)) selections[component] = row;
    }
    return selections;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
