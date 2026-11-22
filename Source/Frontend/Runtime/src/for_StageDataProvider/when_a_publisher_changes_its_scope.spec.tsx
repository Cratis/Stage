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

describe('when a publisher changes its scope', () => {
    async function moved() {
        const view = renderUnderProvider(<><Publisher scope='before' value='row' /><Probe paths={[]} /></>);
        await waitFor(() => resolved('scopes').should.equal('before'));
        view.rerenderUnderProvider(<><Publisher scope='after' value='row' /><Probe paths={[]} /></>);
        return view;
    }

    it('should publish under the new scope', async () => {
        await moved();

        await waitFor(() => resolved('scopes').should.include('after'));
    });

    it('should withdraw the old scope', async () => {
        await moved();

        await waitFor(() => resolved('scopes').should.equal('after'));
    });
});
