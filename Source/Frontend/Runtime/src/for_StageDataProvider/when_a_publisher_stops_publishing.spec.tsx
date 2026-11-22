// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { chai, describe, it, vi } from 'vitest';
import { answerFor, Probe, Publisher, renderUnderProvider, resolved } from './publishers';

chai.should();

vi.mock('../queryExecutor', () => ({
    registeredQuery: () => class { },
    performQuery: async (_type: unknown, args: Record<string, unknown>) => answerFor(args),
    subscribeQuery: () => undefined,
}));

describe('when a publisher stops publishing', () => {
    it('should withdraw its registration, name and alias', async () => {
        const view = renderUnderProvider(<><Publisher scope='orders' name='Orders' alias='Basket' value='order' /><Probe paths={['query.Orders']} /></>);
        await waitFor(() => resolved('results').should.include('Basket'));

        view.rerenderUnderProvider(<><Publisher scope='orders' name='Orders' alias='Basket' publish={false} value='order' /><Probe paths={['query.Orders']} /></>);

        await waitFor(() => resolved('scopes').should.equal(''));
        resolved('results').should.equal('');
        resolved('query.Orders').should.equal('null');
    });
});
