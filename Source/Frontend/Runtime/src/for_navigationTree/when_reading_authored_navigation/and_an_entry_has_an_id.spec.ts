// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { authoredNavigationEntries, navigationTree } from '../../navigationTree';
import { contribution, screen } from '../given/navigation_contributions';

chai.should();

describe('when reading authored navigation and an entry has an id', () => {
    const entry = contribution('Work items', 'WorkItems', ['Work'], { id: 'nav:work-items' });
    const entries = authoredNavigationEntries([screen('A', [entry]), screen('B', [entry])]);

    it('should keep the id on the entry', () => {
        entries[0].id!.should.equal('nav:work-items');
    });

    it('should read the entry once', () => {
        entries.length.should.equal(1);
    });

    it('should keep the id on the tree node', () => {
        navigationTree(entries)[0].children[0].id!.should.equal('nav:work-items');
    });

    it('should ignore an id that is not a string', () => {
        const [bad] = authoredNavigationEntries([screen('A', [contribution('X', 'X', [], { id: 5 })])]);
        (bad.id === undefined).should.equal(true);
    });
});
