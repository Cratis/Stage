// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useMemo, useRef } from 'react';
import type { BindingExpression, ExternalComponent } from '@cratis/scene.model';
import { createBindingResolver, resolveSignalBehaviors } from '@cratis/scene.engine';
import { attachmentFor, useInteractionScope } from '@cratis/scene.react';
import type { ExtensionValues } from '@cratis/scene.extensions';
import { runScreenplayBindings } from './screenplayGuard';
import { useStageData } from './stageData';

/** The binding path a screen reads a value a signal carries with: `signal.<value>`. */
export const signalValuePrefix = 'signal';

/**
 * Resolves the bindings of an interaction that handles a raised signal. A `signal.<value>` path reads the value the component
 * raised the signal with; every other path is the screen's own.
 *
 * @param values What the component raised the signal with.
 * @param fallback How the screen resolves everything else.
 * @returns The resolver to run the interaction with.
 */
export function resolveSignalValues(values: ExtensionValues, fallback: (binding: BindingExpression) => unknown): (binding: BindingExpression) => unknown {
    const resolveValue = createBindingResolver({ dataContext: { [signalValuePrefix]: values } });
    return binding => binding.path === signalValuePrefix || binding.path?.startsWith(`${signalValuePrefix}.`) ? resolveValue(binding) : fallback(binding);
}

/**
 * Runs a screen's `on <signal>` interactions when the extension component they are attached to raises that signal.
 *
 * The bindings are the element's own, resolved by name with `resolveSignalBehaviors` and run by the Scene interaction engine - the same
 * engine that runs every other interaction - so a signal never starts an application trigger of the same name, and the reverse. A page
 * with no interaction scope has no one to run them and ignores the signal. Once they have run the data is read again, since a command
 * the interaction executed may have changed what the screen shows.
 *
 * @param element The extension component element the behaviors are attached to.
 * @returns What to call with the signal the component raised and the values it carried.
 */
export function useSignalBehaviors(element: ExternalComponent): (signal: string, values: ExtensionValues) => void {
    const scope = useInteractionScope();
    const data = useStageData();
    const attachments = useMemo(() => attachmentFor(element.id, element.behaviors), [element.behaviors, element.id]);
    const latest = useRef({ scope, data, attachments });
    latest.current = { scope, data, attachments };

    return useCallback((signal: string, values: ExtensionValues) => {
        const { scope: current, data: stage, attachments: attached } = latest.current;
        if (!current) return;
        const bindings = resolveSignalBehaviors(attached, signal);
        if (bindings.length === 0) return;
        const context = { ...current.context, resolve: resolveSignalValues(values, current.context.resolve) };
        void runScreenplayBindings(bindings, current.dispatcher, context).then(result => {
            if (result.findings.length > 0) current.onFindings?.(result.findings);
            stage.refreshAfterCommand();
        });
    }, []);
}
