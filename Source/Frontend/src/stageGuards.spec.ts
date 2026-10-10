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
