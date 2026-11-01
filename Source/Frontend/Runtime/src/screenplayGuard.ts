// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveSubjectReference, runBindings } from '@cratis/scene.engine';
import { GuardOperator } from '@cratis/scene.model';

/** Runs every Stage-owned interaction through Screenplay admission before native Scene selection. */
export function runScreenplayBindings(...[bindings, dispatcher, context]: Parameters<typeof runBindings>): ReturnType<typeof runBindings> {
    const item = context.subject?.() ?? context.resolve({ path: 'item' });
    return runBindings(bindings.map(candidate => ({ ...candidate, binding: screenplayGuardBinding(candidate.binding, item) })), dispatcher, context);
}

/** Restricts native Scene guards to Screenplay's string-only contains contract before branch selection. */
export function screenplayGuard<Condition>(condition: Condition, item: unknown): Condition {
    if (!record(condition)) return condition;
    if (condition.kind === 'logical') return {
        ...condition,
        left: screenplayGuard(condition.left, item),
        right: screenplayGuard(condition.right, item),
    };
    if (condition.kind !== 'comparison' || condition.operator !== GuardOperator.Contains) return condition;
    const value = subjectValue(condition.left, item);
    if (typeof value === 'string' && typeof condition.right === 'string') return condition;
    // An invalid subject keeps Scene's existing unevaluable diagnostic and suppresses otherwise, too.
    return { ...condition, left: { subject: '' } };
}

/** Applies the same admission rule to every alternative, including those native selection would not reach. */
export function screenplayGuardAlternatives(alternatives: unknown, item: unknown): unknown {
    return Array.isArray(alternatives) ? alternatives.map(alternative => record(alternative)
        ? { ...alternative, condition: screenplayGuard(alternative.condition, item) }
        : alternative) : alternatives;
}

/** Restricts a binding without changing its native Scene sequencing or fallback actions. */
export function screenplayGuardBinding<Binding extends { alternatives?: unknown }>(binding: Binding, item: unknown): Binding {
    return { ...binding, alternatives: screenplayGuardAlternatives(binding.alternatives, item) };
}

function subjectValue(reference: unknown, item: unknown): unknown {
    return record(reference) && typeof reference.subject === 'string'
        ? resolveSubjectReference({ subject: reference.subject }, item)
        : undefined;
}

function record(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
