// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { authoredNavigationEntries } from '../../navigationTree';
import { contribution, screen } from '../given/navigation_contributions';

chai.should();

describe('when reading authored navigation and two entries open one screen under different ids', () => {
    const entries = authoredNavigationEntries([screen('A', [
        contribution('Work items', 'WorkItems', ['Work'], { id: 'a' }),
        contribution('Work items', 'WorkItems', ['Work'], { id: 'b' }),
    ])]);

    it('should read both entries', () => {
        entries.map(_ => _.id).should.deep.equal(['a', 'b']);
    });
});
