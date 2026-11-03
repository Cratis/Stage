// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { canEvaluate, evaluateCondition, firstMatch, resolveGuard } from './stageGuards';

const open = { kind: 'comparison', left: { path: 'item.status' }, operator: 'Equal', right: 'open' };
const many = { kind: 'comparison', left: { path: 'item.count' }, operator: 'GreaterThan', right: 3 };

describe('a guard', () => {
    it('holds when the field equals the literal', () => expect(evaluateCondition(open, { status: 'open' })).toBe(true));
    it('does not hold when the field differs', () => expect(evaluateCondition(open, { status: 'closed' })).toBe(false));
    it('does not hold on a missing field', () => expect(evaluateCondition(open, {})).toBe(false));
    it('does not hold on a null field, even for not-equal', () =>
        expect(evaluateCondition({ ...open, operator: 'NotEqual' }, { status: null })).toBe(false));
    it('orders only numbers', () => {
        expect(evaluateCondition(many, { count: 4 })).toBe(true);
        expect(evaluateCondition(many, { count: '4' })).toBe(false);
    });
    it('matches text and list contents and prefixes', () => {
        expect(evaluateCondition({ ...open, operator: 'Contains', right: 'pe' }, { status: 'open' })).toBe(true);
        expect(evaluateCondition({ ...open, left: { path: 'item.tags' }, operator: 'Contains', right: 'x' }, { tags: ['x'] })).toBe(true);
        expect(evaluateCondition({ ...open, operator: 'StartsWith', right: 'op' }, { status: 'open' })).toBe(true);
    });
    it('combines with and and or', () => {
        expect(evaluateCondition({ kind: 'logical', left: open, operator: 'And', right: many }, { status: 'open', count: 1 })).toBe(false);
        expect(evaluateCondition({ kind: 'logical', left: open, operator: 'Or', right: many }, { status: 'open', count: 1 })).toBe(true);
    });
    it('reads nested fields', () =>
        expect(evaluateCondition({ ...open, left: { path: 'item.owner.name' } }, { owner: { name: 'open' } })).toBe(true));

    describe('that cannot be evaluated', () => {
        const cases: [string, unknown][] = [
            ['an unknown condition kind', { kind: 'ComparisonWithPathSyntax' }],
            ['an unknown operator', { ...open, operator: 'Matches' }],
            ['a non-literal right side', { ...open, right: { kind: 'PathExpressionSyntax' } }],
            ['a left side that is not about the item', { ...open, left: { path: 'state.status' } }],
            ['a logical condition with an unevaluable side', { kind: 'logical', left: open, operator: 'And', right: { kind: 'x' } }],
            ['an unknown logical operator', { kind: 'logical', left: open, operator: 'Xor', right: open }],
        ];
        for (const [name, condition] of cases) {
            it(`is undecided for ${name}, never true`, () => {
                expect(evaluateCondition(condition, { status: 'open' })).toBeUndefined();
                expect(canEvaluate([open, condition])).toBe(false);
            });
        }
    });
});

const numberedComparison = (right: unknown = 'open', operator = 'Equal') => ({
    kind: 'comparison', left: { subject: 'item.status' }, operator, right,
    enumeration: ['open', 'closed'], enumerationNumbers: [1, 0],
});
const numberedSelection = (conditions: unknown[], kind = 'firstMatch', index = 0) => ({ kind, index, alternatives: conditions });

describe('when comparing explicitly numbered enumeration members', () => {
    it('should use reordered identity rather than ordinal or crossed identity', () => {
        expect(evaluateCondition(numberedComparison(), { status: 1 })).toBe(true);
        expect(evaluateCondition(numberedComparison(), { status: 0 })).toBe(false);
        expect(evaluateCondition(numberedComparison('closed'), { status: 0 })).toBe(true);
        expect(evaluateCondition(numberedComparison('closed'), { status: 1 })).toBe(false);
        expect(evaluateCondition(numberedComparison('closed', 'NotEqual'), { status: 0 })).toBe(false);
        expect(evaluateCondition(numberedComparison('closed', 'NotEqual'), { status: 1 })).toBe(true);
    });
    it('should preserve sparse negative numbers without accepting the ordinal', () => {
        const condition = { ...numberedComparison('draft'), enumeration: ['draft', 'issued', 'cancelled'], enumerationNumbers: [-1, 4, 90] };
        expect(evaluateCondition(condition, { status: -1 })).toBe(true);
        expect(evaluateCondition(condition, { status: 0 })).toBe(false);
        expect(evaluateCondition({ ...condition, right: 'cancelled' }, { status: 90 })).toBe(true);
        expect(evaluateCondition({ ...condition, operator: 'NotEqual' }, { status: 0 })).toBe(false);
    });
    it('should work in both mixed primitive directions and preserve case-insensitive names', () => {
        expect(evaluateCondition(numberedComparison(1), { status: 'open' })).toBe(true);
        expect(evaluateCondition(numberedComparison(0), { status: 'open' })).toBe(false);
        expect(evaluateCondition(numberedComparison('OPEN'), { status: 'Open' })).toBe(true);
    });
    it('should fail closed on unknown explicit values for both equality operators and directions', () => {
        for (const operator of ['Equal', 'NotEqual']) {
            expect(evaluateCondition(numberedComparison('open', operator), { status: 90 })).toBe(false);
            expect(evaluateCondition(numberedComparison(90, operator), { status: 'open' })).toBe(false);
            expect(evaluateCondition(numberedComparison('missing', operator), { status: 1 })).toBe(false);
        }
    });
    it('should compose logically and select the first alternative', () => {
        const conditions = [numberedComparison(), numberedComparison('closed')];
        expect(evaluateCondition({ kind: 'logical', operator: 'And', left: conditions[0], right: numberedComparison('closed', 'NotEqual') }, { status: 1 })).toBe(true);
        expect(firstMatch(conditions, { status: 0 })).toBe(1);
        expect(resolveGuard(numberedSelection(conditions, 'firstMatch', 1), { status: 0 })).toBe(true);
        expect(resolveGuard(numberedSelection(conditions, 'otherwise'), { status: 0 })).toBe(false);
    });
    it('should retain implicit ordinals and strict untyped equality', () => {
        const implicit = { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'open', enumeration: ['open', 'closed'] };
        const untyped = { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'open' };
        expect(evaluateCondition(implicit, { status: 0 })).toBe(true);
        expect(evaluateCondition(untyped, { status: 'open' })).toBe(true);
        expect(evaluateCondition(untyped, { status: 0 })).toBe(false);
    });
});

const malformedEnumerationNumbers = [
    { enumerationNumbers: [1, 1] }, { enumerationNumbers: [1] }, { enumerationNumbers: [1.5, 0] },
    { enumerationNumbers: ['1', 0] }, { enumerationNumbers: null }, { enumerationNumbers: 1 },
    { enumerationNumbers: [Number.NaN, 0] }, { enumerationNumbers: [Number.POSITIVE_INFINITY, 0] },
    { enumeration: undefined }, { enumeration: null }, { enumeration: ['open', 0] },
    { enumerationNumbers: undefined },
];

describe('when enumeration number metadata is malformed', () => {
    for (const [index, metadata] of malformedEnumerationNumbers.entries()) {
        it(`should fail closed for malformed mapping ${index}`, () => {
            const condition = { ...numberedComparison(), ...metadata };
            expect(canEvaluate([condition])).toBe(false);
            expect(evaluateCondition(condition, { status: 1 })).toBeUndefined();
            expect(firstMatch([condition], { status: 1 })).toBeUndefined();
            expect(resolveGuard(numberedSelection([condition]), { status: 1 })).toBeUndefined();
            expect(resolveGuard(numberedSelection([condition], 'otherwise'), { status: 1 })).toBeUndefined();
            for (const operator of ['And', 'Or']) {
                expect(evaluateCondition({ kind: 'logical', operator, left: numberedComparison(), right: condition }, { status: 1 })).toBeUndefined();
            }
        });
    }
});

describe('first-match alternatives', () => {
    const closed = { ...open, right: 'closed' };

    it('picks the first that holds', () => expect(firstMatch([closed, open, open], { status: 'open' })).toBe(1));
    it('says none holds', () => expect(firstMatch([closed], { status: 'open' })).toBe(-1));
    it('is undecided when any alternative is unevaluable', () => expect(firstMatch([open, { kind: 'x' }], { status: 'open' })).toBeUndefined());
    it('runs only the branch that matched', () => {
        expect(resolveGuard({ kind: 'firstMatch', index: 0, alternatives: [open] }, { status: 'open' })).toBe(true);
        expect(resolveGuard({ kind: 'otherwise', alternatives: [open] }, { status: 'open' })).toBe(false);
        expect(resolveGuard({ kind: 'otherwise', alternatives: [open] }, { status: 'closed' })).toBe(true);
    });
    it('runs neither branch without a subject', () => {
        expect(resolveGuard({ kind: 'firstMatch', index: 0, alternatives: [open] }, undefined)).toBeUndefined();
        expect(resolveGuard({ kind: 'otherwise', alternatives: [open] }, undefined)).toBeUndefined();
    });
});
