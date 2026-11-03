// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { canEvaluateGuard, evaluateGuard, selectGuardedInteraction } from '@cratis/scene.engine';

const open = { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'open' };
const many = { kind: 'comparison', left: { subject: 'item.count' }, operator: 'GreaterThan', right: 3 };

describe('Scene native guards over explicitly numbered enumerations', () => {
    const comparison = (members: string[], numbers: unknown, right = 'open', operator = 'Equal') => ({
        kind: 'comparison', left: { subject: 'item.status' }, operator, right, enumeration: members, enumerationNumbers: numbers,
    });
    const reordered = ['open', 'closed'];
    const sparse = ['draft', 'issued', 'cancelled'];
    const numberedComparison = (right: unknown = 'open', operator = 'Equal') => ({
        ...comparison(reordered, [1, 0]), right, operator,
    });

    it.each([
        [comparison(reordered, [1, 0]), 1, true],
        [comparison(reordered, [1, 0]), 0, false],
        [comparison(reordered, [1, 0], 'open', 'NotEqual'), 0, true],
        [comparison(sparse, [-1, 4, 90], 'draft'), -1, true],
        [comparison(sparse, [-1, 4, 90], 'issued'), 4, true],
        [comparison(sparse, [-1, 4, 90], 'issued'), 1, false],
        [comparison(sparse, [-1, 4, 90], 'issued'), 7, false],
        [comparison(sparse, [-1, 4, 90], 'issued', 'NotEqual'), 7, false],
        [comparison(sparse, [-1, 4, 90], 'issued'), 'ISSUED', true],
    ])('uses explicit member identity for %j with wire value %s', (condition, status, expected) => {
        expect(evaluateGuard(condition, { status })).toBe(expected);
    });

    it.each([[1, 1], [1], [1.5, 0], ['1', 0], null, 1, [Infinity, 0], [NaN, 0]].map(numbers => ({ numbers })))('refuses malformed numbers $numbers', ({ numbers }) => {
        const condition = comparison(reordered, numbers);
        expect(canEvaluateGuard(condition)).toBe(false);
        expect(evaluateGuard(condition, { status: 1 })).toBeUndefined();
        expect(selectGuardedInteraction([{ condition }], { status: 1 }, true).kind).toBe('unevaluable');
        expect(canEvaluateGuard({ kind: 'logical', operator: 'Or', left: condition, right: comparison(reordered, [1, 0]) })).toBe(false);
    });

    it.each(['Equal', 'NotEqual'])('refuses unknown member identities for %s', operator => {
        expect(evaluateGuard(numberedComparison(90, operator), { status: 'open' })).toBe(false);
        expect(evaluateGuard(numberedComparison('missing', operator), { status: 1 })).toBe(false);
    });

    it('combines numbered conditions and selects the first match', () => {
        const conditions = [numberedComparison(), numberedComparison('closed')];
        expect(evaluateGuard({ kind: 'logical', operator: 'And', left: conditions[0], right: numberedComparison('closed', 'NotEqual') }, { status: 1 })).toBe(true);
        expect(selectGuardedInteraction(conditions.map(condition => ({ condition })), { status: 0 }, true)).toEqual({ kind: 'alternative', index: 1 });
    });

    it.each([undefined, null, ['open', 0]])('refuses invalid member names %j', enumeration => {
        const condition = { ...numberedComparison(), enumeration };
        expect(canEvaluateGuard(condition)).toBe(false);
        expect(evaluateGuard(condition, { status: 1 })).toBeUndefined();
        expect(selectGuardedInteraction([{ condition }], { status: 1 }, true).kind).toBe('unevaluable');
    });
});

// These run the published engine, not a second Stage implementation of its semantics.
describe('Scene guard execution', () => {
    it('compares the explicit subject', () => expect(evaluateGuard(open, { status: 'open' })).toBe(true));
    it('does not match a different field value', () => expect(evaluateGuard(open, { status: 'closed' })).toBe(false));
    it('does not match a missing field', () => expect(evaluateGuard(open, {})).toBe(false));
    it('compares finite numbers, not numeric strings', () => {
        expect(evaluateGuard(many, { count: 4 })).toBe(true);
        expect(evaluateGuard(many, { count: '4' })).toBe(false);
    });
    it('keeps logical operands as closed predicates', () => {
        expect(evaluateGuard({ kind: 'logical', left: open, operator: 'And', right: many }, { status: 'open', count: 4 })).toBe(true);
        expect(evaluateGuard({ kind: 'logical', left: open, operator: 'Or', right: many }, { status: 'closed', count: 4 })).toBe(true);
    });
    it('checks enum membership', () => {
        const condition = { ...open, enumeration: ['open', 'closed'] };
        expect(evaluateGuard(condition, { status: 'open' })).toBe(true);
        expect(evaluateGuard(condition, { status: 'unknown' })).toBe(false);
    });
    it.each([
        { kind: 'unknown' },
        { ...open, left: { subject: 'screen.status' } },
        { ...open, left: { subject: 'item.constructor' } },
        { ...open, operator: 'Mystery' },
        { ...open, right: { binding: 'item.x' } },
    ])('refuses an unsupported predicate without taking otherwise: %j', condition => {
        expect(canEvaluateGuard(condition)).toBe(false);
        expect(selectGuardedInteraction([{ condition: open }, { condition }], { status: 'open' }, true).kind).toBe('unevaluable');
    });
    it('chooses exactly the first matching branch', () => expect(selectGuardedInteraction([{ condition: open }, { condition: open }], { status: 'open' }, true)).toEqual({ kind: 'alternative', index: 0 }));
    it('uses otherwise only for an unmatched subject', () => expect(selectGuardedInteraction([{ condition: open }], { status: 'closed' }, true).kind).toBe('otherwise'));
    it('does not use otherwise without a subject', () => expect(selectGuardedInteraction([{ condition: open }], undefined, true).kind).toBe('noSubject'));
});
