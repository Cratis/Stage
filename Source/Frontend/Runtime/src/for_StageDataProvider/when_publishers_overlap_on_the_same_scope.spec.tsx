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

describe('when publishers overlap on the same scope', () => {
    async function handedOver() {
        const view = renderUnderProvider(<><Publisher key='older' scope='shared' name='Shared' value='older' /><Probe paths={['query.Shared.value']} /></>);
        await waitFor(() => resolved('query.Shared.value').should.equal('"older"'));

        // The newer publisher mounts and publishes before the older one's cleanup runs.
        view.rerenderUnderProvider(<><Publisher key='newer' scope='shared' name='Shared' value='newer' /><Probe paths={['query.Shared.value']} /></>);
        return view;
    }

    it('should keep the newer publisher registered after the older one is cleaned up', async () => {
        await handedOver();

        await waitFor(() => resolved('query.Shared.value').should.equal('"newer"'));
        resolved('scopes').should.equal('shared');
    });

    it('should withdraw it when the newer publisher unmounts', async () => {
        const view = await handedOver();
        await waitFor(() => resolved('query.Shared.value').should.equal('"newer"'));

        view.rerenderUnderProvider(<Probe paths={['query.Shared.value']} />);

        await waitFor(() => resolved('scopes').should.equal(''));
    });
});
