// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { applyQueryChangeSet } from '../applyQueryChangeSet';

class Identifier {
    constructor(readonly value: string) {}
    equals(other: unknown) { return other instanceof Identifier && other.value === this.value; }
}

describe('when applying collection deltas', () => {
    it('matches deserialized concept identifiers by value', () => {
        const retained = { id: new Identifier('keep'), name: 'Retained' };
        const replacement = { id: new Identifier('replace'), name: 'After' };
        expect(applyQueryChangeSet([retained, { id: new Identifier('replace'), name: 'Before' }, { id: new Identifier('remove') }], {
            added: [], replaced: [replacement], removed: [{ id: new Identifier('remove') }],
        })).toEqual([retained, replacement]);
    });
    it('adds and removes collections of primitives without identifiers', () => {
        expect(applyQueryChangeSet(['keep', 'remove'], { added: ['add'], replaced: [], removed: ['remove'] })).toEqual(['keep', 'add']);
    });
    it('matches object identifiers by structural value when they have no equals method', () => {
        expect(applyQueryChangeSet([{ id: { value: 'remove' } }], { added: [], replaced: [], removed: [{ id: { value: 'remove' } }] })).toEqual([]);
    });
    it('does not remove unchanged rows on an additions-only delta', () => {
        expect(applyQueryChangeSet([{ id: 1 }], { added: [{ id: 2 }], replaced: [], removed: [] })).toEqual([{ id: 1 }, { id: 2 }]);
    });
});
