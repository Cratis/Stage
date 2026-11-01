// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { authoredNavigationEntries, navigationPathTo, navigationTree } from '../../navigationTree';
import { contribution, screen } from '../given/navigation_contributions';

chai.should();

describe('when reading authored navigation and an entry has a group', () => {
    const entries = authoredNavigationEntries([screen('A', [
        contribution('Work items', 'WorkItems', ['Work'], { group: 'Tracking' }),
        contribution('Boards', 'Boards', ['Work'], { group: 'Tracking' }),
        contribution('Home', 'Home', ['Work']),
    ])]);
    const tree = navigationTree(entries);
    const work = tree[0];

    it('should keep the group on the entry', () => {
        entries[0].group!.should.equal('Tracking');
    });

    it('should place the entries sharing a group under one section heading', () => {
        work.children.map(_ => _.label).should.deep.equal(['Tracking', 'Home']);
        work.children[0].children.map(_ => _.label).should.deep.equal(['Work items', 'Boards']);
    });

    it('should leave the section out of the breadcrumb', () => {
        navigationPathTo(tree, 'Boards').should.deep.equal(['Work', 'Boards']);
    });

    it('should not merge a section with a feature of the same name', () => {
        const merged = navigationTree(authoredNavigationEntries([screen('A', [
            contribution('One', 'One', ['Work', 'Tracking']),
            contribution('Two', 'Two', ['Work'], { group: 'Tracking' }),
        ])]));
        merged[0].children.length.should.equal(2);
    });

    it('should ignore a group that is not a string', () => {
        const [bad] = authoredNavigationEntries([screen('A', [contribution('X', 'X', [], { group: 5 })])]);
        (bad.group === undefined).should.equal(true);
    });
});
