// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { authoredNavigationEntries, navigationPathTo, navigationTree } from '../../navigationTree';
import { contribution, screen } from '../given/navigation_contributions';

chai.should();

describe('when reading authored navigation and an entry opens an external route', () => {
    const destination = { kind: 'external', route: 'https://cratis.io/help', routeParameterBindings: {} };
    const entries = authoredNavigationEntries([screen('A', [contribution('Work items', 'Help', ['Work', 'Items'], { destination })])]);
    const tree = navigationTree(entries);

    it('should keep the destination on the entry', () => {
        entries[0].destination!.route!.should.equal('https://cratis.io/help');
    });

    it('should keep the destination on the tree node', () => {
        tree[0].children[0].children[0].destination!.kind!.should.equal('external');
    });

    it('should keep the breadcrumb path', () => {
        navigationPathTo(tree, 'Help').should.deep.equal(['Work', 'Items', 'Work items']);
    });
});
