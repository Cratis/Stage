// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExternalComponent, InteractionAction } from '@cratis/scene.model';
import { commandResponseContext, runActions, validateResponseBindings } from '@cratis/scene.engine';
import type { InteractionScopeValue } from '@cratis/scene.react';

type Completed = (messages: string[]) => void;
type Continuation = { messages: string[]; complete: (response: unknown, completed: Completed) => void };
const noContinuation: Continuation = { messages: [], complete: (_response, completed) => completed([]) };

/** Admits a form's successful-submit actions before the command can commit, then runs them in an invocation-local Scene response context. */
export function formContinuation(element: ExternalComponent, form: { command: string } | undefined, scope: InteractionScopeValue | undefined): Continuation {
    const actions = element.properties.onSubmit as InteractionAction[] | undefined;
    if (!actions?.length) return noContinuation;
    if (!scope) return { ...noContinuation, messages: ['The form submit continuation requires an interaction scope.'] };
    const command = form?.command ?? '';
    const contracts = scope.context.responseContracts ?? { commandResponse: () => ({ kind: 'void' as const }), targetType: () => undefined };
    const diagnostics = validateResponseBindings({ kind: 'form', command, onSubmit: actions }, contracts);
    const messages = diagnostics.map(diagnostic => diagnostic.message);
    return {
        messages,
        complete: async (response, completed) => {
            const context = commandResponseContext(scope.context, command, response);
            const result = await runActions(actions, scope.dispatcher, context);
            if (result.findings.length) scope.onFindings?.(result.findings);
            completed(result.findings.map(finding => finding.detail));
        },
    };
}
