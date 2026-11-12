// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { reactComponent } from '@cratis/scene.extensions';
import { aHost, aPlacedComponent, placement } from './given/a_placed_component';

function Chart() {
    return <span data-testid='shown'>shown</span>;
}

const importer = async () => ({ Chart: reactComponent(Chart) });

describe('when a screen that places a component is shown', () => {
    it('tells the host which screen was reached so the backend of the system can start', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        await view.findByTestId('shown');
        const reached = host.calls.filter(call => call.url === 'stage/extensions/activation/screens/Dashboard');
        expect(reached.map(call => call.method)).toEqual(['POST']);
    });

    it('carries nothing but the screen', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        await view.findByTestId('shown');
        expect(host.calls.find(call => call.url.includes('/activation/screens/'))!.body).toBeUndefined();
    });

    it('still shows the component when the host cannot be told', async () => {
        const host = aHost();
        const fetcher = (async (input: RequestInfo | URL, init?: RequestInit) => {
            if (String(input).includes('/activation/screens/')) throw new Error('offline');
            return host.fetcher(input, init);
        }) as typeof fetch;
        const view = aPlacedComponent(placement(), { fetcher, importer });
        expect((await view.findByTestId('shown')).textContent).toBe('shown');
    });
});
