// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { chai, describe, it } from 'vitest';
import { StageNavigationTree } from '../StageChrome';
import { authoredNavigationEntries, navigationTree } from '../navigationTree';
import { contribution, screen } from '../for_navigationTree/given/navigation_contributions';

chai.should();

describe('when rendering a grouped entry', () => {
    const nodes = navigationTree(authoredNavigationEntries([screen('A', [
        contribution('Work items', 'WorkItems', ['Work'], { group: 'Tracking' }),
    ])]));
    const renderTree = () => render(<StageNavigationTree element={{ id: 'nav', name: 'nav', componentName: 'Stage:navigation', properties: { nodes }, slots: {} } as never} slots={{}} />).container;

    it('should show the group as a section heading', () => {
        Array.from(renderTree().querySelectorAll('.stage-navigation-group')).map(_ => _.textContent).should.deep.equal(['Work', 'Tracking']);
    });

    it('should list the entry link inside the section', () => {
        const section = renderTree().querySelectorAll('li[aria-expanded]')[1];
        section.querySelector('a')!.textContent!.should.equal('Work items');
    });
});
