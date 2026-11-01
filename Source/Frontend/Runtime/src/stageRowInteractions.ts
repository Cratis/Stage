// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef } from 'react';
import type { BindingExpression, ExternalComponent } from '@cratis/scene.model';
import { InteractionTriggerKind } from '@cratis/scene.model';
import { resolveBehaviors, resolveSubjectReference } from '@cratis/scene.engine';
import { runScreenplayBindings } from './screenplayGuard';
import { attachmentFor, useInteractionScope } from '@cratis/scene.react';

/** How long a row waits for a second click before treating the first as a single click. */
export const doubleClickWindow = 250;

/** What a table does with clicks on its rows. */
export interface RowInteractions {
    /** Handles a click: at once, or after the double-click window when the table also handles double clicks. */
    click: (row: Record<string, unknown>, single: () => void) => void;

    /** Handles a double click, when the table declares one; otherwise `undefined`. */
    doubleClick?: (row: Record<string, unknown>) => void;
}

/**
 * Runs a table's `on double click` bindings for the row that was double clicked.
 *
 * The bindings are the table's own, resolved and run by the Scene interaction engine - the same engine that runs
 * every other interaction - with the subject meaning the row. Scene selects the first matching alternative
 * and runs the otherwise actions only when none match.
 *
 * A row's single click (selection, or navigation to the row) would otherwise always win the race with a double
 * click, because a double click starts with a click. When the table declares a double click, a single click waits
 * out the double-click window first and is cancelled by the double click.
 *
 * @param element The table.
 * @returns The row handlers.
 */
export function useRowInteractions(element: ExternalComponent): RowInteractions {
    const scope = useInteractionScope();
    const pending = useRef<ReturnType<typeof setTimeout>>(undefined);
    const bindings = useMemo(
        () => resolveBehaviors(attachmentFor(element.id, element.behaviors), InteractionTriggerKind.DoubleClick),
        [element.behaviors, element.id]);
    useEffect(() => () => clearTimeout(pending.current), []);

    const doubleClick = useCallback((row: Record<string, unknown>) => {
        clearTimeout(pending.current);
        pending.current = undefined;
        if (!scope) return;
        const resolve = (binding: BindingExpression): unknown => {
            if (binding.path?.startsWith('item.')) return resolveSubjectReference({ subject: binding.path }, row);
            return scope.context.resolve(binding);
        };
        void runScreenplayBindings(bindings, scope.dispatcher, { ...scope.context, subject: () => row, resolve }).then(result => {
            if (result.findings.length > 0) scope.onFindings?.(result.findings);
        });
    }, [bindings, scope]);

    const click = useCallback((_row: Record<string, unknown>, single: () => void) => {
        if (bindings.length === 0) {
            single();
            return;
        }

        clearTimeout(pending.current);
        pending.current = setTimeout(() => {
            pending.current = undefined;
            single();
        }, doubleClickWindow);
    }, [bindings.length]);

    return { click, doubleClick: bindings.length > 0 ? doubleClick : undefined };
}
