// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Evaluates the guards a Screenplay `when ... otherwise` carries.
 *
 * A guard compares a field of the subject - `item`, the record the nearest `data` declaration reads, or the row an
 * interaction happened on - with a literal, and combines comparisons with `and` and `or`. That is the whole guard
 * language Screenplay admits, and the same shape Stage emits into the Scene for a guarded action and for a guarded
 * interaction. Anything else is not evaluable: the answer is then `undefined`, never `true`, so a guard the runtime
 * does not understand can never let a command through.
 */

/** The binding path a guarded interaction binding's condition carries; its `value` is a {@link GuardSelection}. */
export const guardPath = '$guard';

/** One condition of a guard. */
export type GuardCondition = ComparisonCondition | LogicalCondition;

/** Compares a field of the subject with a literal. */
export interface ComparisonCondition {
    kind: 'comparison';
    left: { path: string };
    operator: string;
    right: unknown;
}

/** Combines two conditions. */
export interface LogicalCondition {
    kind: 'logical';
    left: GuardCondition;
    operator: string;
    right: GuardCondition;
}

/**
 * Which branch of an ordered first-match guard a binding is: alternative `index`, or the `otherwise` that runs when
 * there is a subject and no alternative holds.
 */
export interface GuardSelection {
    kind: 'firstMatch' | 'otherwise';
    index?: number;
    alternatives: unknown[];
}

/** The diagnostic a guard the runtime cannot evaluate is reported with. */
export const unevaluableGuardCode = 'STAGE-SCENE-ACTION-001';

const subjectPrefix = 'item.';

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function valueAt(subject: Record<string, unknown>, path: string): unknown {
    let current: unknown = subject;
    for (const part of path.split('.').filter(Boolean)) {
        if (!isRecord(current)) return undefined;
        current = current[part];
    }
    return current;
}

function isLiteral(value: unknown): boolean {
    return value === null || ['string', 'number', 'boolean'].includes(typeof value);
}

function compare(field: unknown, operator: string, literal: unknown): boolean | undefined {
    switch (operator) {
        case 'Equal': return field !== undefined && field !== null && field === literal;
        case 'NotEqual': return field !== undefined && field !== null && field !== literal;
        case 'GreaterThan': return typeof field === 'number' && typeof literal === 'number' && field > literal;
        case 'GreaterThanOrEqual': return typeof field === 'number' && typeof literal === 'number' && field >= literal;
        case 'LessThan': return typeof field === 'number' && typeof literal === 'number' && field < literal;
        case 'LessThanOrEqual': return typeof field === 'number' && typeof literal === 'number' && field <= literal;
        case 'Contains':
            if (typeof literal !== 'string') return false;
            if (typeof field === 'string') return field.includes(literal);
            return Array.isArray(field) && field.includes(literal);
        case 'StartsWith': return typeof field === 'string' && typeof literal === 'string' && field.startsWith(literal);
        default: return undefined;
    }
}

/**
 * Evaluates one condition against a subject.
 *
 * A missing or null field does not match - `item.status == "open"` is false for an item without a status - because
 * that is what Screenplay specifies and because the alternative, matching on absence, would run a command against
 * data that never said it could.
 *
 * @param condition The condition as the Scene carries it.
 * @param subject The record the condition is about.
 * @returns Whether it holds, or `undefined` when it is not a condition this runtime can evaluate.
 */
export function evaluateCondition(condition: unknown, subject: Record<string, unknown>): boolean | undefined {
    if (!isRecord(condition)) return undefined;
    if (condition.kind === 'comparison') {
        const left = isRecord(condition.left) ? condition.left.path : undefined;
        if (typeof left !== 'string' || !left.startsWith(subjectPrefix) || typeof condition.operator !== 'string' || !isLiteral(condition.right)) return undefined;
        return compare(valueAt(subject, left.slice(subjectPrefix.length)), condition.operator, condition.right);
    }

    if (condition.kind === 'logical') {
        const left = evaluateCondition(condition.left, subject);
        const right = evaluateCondition(condition.right, subject);
        if (left === undefined || right === undefined) return undefined;
        if (condition.operator === 'And') return left && right;
        if (condition.operator === 'Or') return left || right;
        return undefined;
    }

    return undefined;
}

/** Whether every condition can be evaluated, independently of any subject. */
export function canEvaluate(conditions: unknown[]): boolean {
    return conditions.every(condition => evaluateCondition(condition, {}) !== undefined);
}

/**
 * Picks the first alternative whose condition holds.
 * @param conditions The alternatives' conditions, in authored order.
 * @param subject The record they are about.
 * @returns The index of the first that holds, -1 when none does, or `undefined` when any is not evaluable.
 */
export function firstMatch(conditions: unknown[], subject: Record<string, unknown>): number | undefined {
    if (!canEvaluate(conditions)) return undefined;
    return conditions.findIndex(condition => evaluateCondition(condition, subject) === true);
}

/**
 * Resolves a guarded interaction binding's condition: whether its branch is the one that runs for this subject.
 * @param selection The {@link GuardSelection} the binding carries.
 * @param subject The record the interaction is about, when there is one.
 * @returns Whether the branch runs, or `undefined` when there is no subject or the guard is not evaluable - which
 * the interaction engine reports and treats as not running.
 */
export function resolveGuard(selection: unknown, subject: unknown): boolean | undefined {
    if (!isRecord(selection) || !Array.isArray(selection.alternatives) || !isRecord(subject)) return undefined;
    const match = firstMatch(selection.alternatives, subject);
    if (match === undefined) return undefined;
    if (selection.kind === 'firstMatch' && typeof selection.index === 'number') return match === selection.index;
    if (selection.kind === 'otherwise') return match === -1;
    return undefined;
}

/** Resolves a binding path against a subject, for paths that start with `item.`. */
export function subjectValue(path: string, subject: unknown): unknown {
    if (!path.startsWith(subjectPrefix) || !isRecord(subject)) return undefined;
    return valueAt(subject, path.slice(subjectPrefix.length));
}
