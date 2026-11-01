// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DestinationKind, type ExternalComponent } from '@cratis/scene.model';
import { render } from '@testing-library/react';
import { chai, describe, it } from 'vitest';
import { StageNavigationTree } from '../StageChrome';
import type { NavigationNode } from '../navigationTree';

chai.should();

function renderEntry(destination?: NavigationNode['destination']) {
    const nodes: NavigationNode[] = [{ label: 'Documentation', screen: 'Documentation', children: [], destination }];
    const element = { id: 'navigation', componentName: 'Stage:navigation-tree', properties: { nodes }, slots: {} } as unknown as ExternalComponent;
    return render(<StageNavigationTree element={element} slots={{}} />).getByText('Documentation');
}

describe('when rendering an external route', () => {
    it.each(['http://example.com/docs', 'https://example.com/docs', 'HTTPS://example.com/docs'])('should keep the allowed route %s', route => {
        const entry = renderEntry({ kind: DestinationKind.External, route });

        entry.getAttribute('href')!.should.equal(route);
        entry.hasAttribute('aria-disabled').should.be.false;
    });

    it.each([
        'data:text/html,<h1>Example</h1>',
        'foo:example',
        'javascript:alert(1)',
        'vbscript:msgbox(1)',
        '//example.com/docs',
        '/docs',
        'not a URL',
        'https://',
        '',
        undefined,
    ])('should keep the item without an href for the refused route %s', route => {
        const entry = renderEntry({ kind: DestinationKind.External, route });

        entry.hasAttribute('href').should.be.false;
        entry.getAttribute('aria-disabled')!.should.equal('true');
        entry.hasAttribute('rel').should.be.false;
    });

    it('should keep an internal screen link unchanged', () => {
        const entry = renderEntry();

        entry.getAttribute('href')!.should.equal('#/Documentation');
    });
});
