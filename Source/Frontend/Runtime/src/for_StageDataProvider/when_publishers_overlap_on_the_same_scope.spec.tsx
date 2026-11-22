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

const probe = <Probe paths={['query.Shared.value']} />;
const older = <Publisher key='older' scope='shared' name='Shared' value='older' />;
const newer = <Publisher key='newer' scope='shared' name='Shared' value='newer' />;

describe('when publishers overlap on the same scope', () => {
    async function olderUnmounted() {
        const view = renderUnderProvider(<>{older}{probe}</>);
        await waitFor(() => resolved('query.Shared.value').should.equal('"older"'));

        // Both are mounted; the newer one's publication replaces the older one's registration.
        view.rerenderUnderProvider(<>{older}{newer}{probe}</>);
        await waitFor(() => resolved('query.Shared.value').should.equal('"newer"'));

        // Only the older one goes away: its stale cleanup must not take the newer result with it.
        view.rerenderUnderProvider(<>{newer}{probe}</>);
        return view;
    }

    it('should keep the newer publisher registered after the older one unmounts', async () => {
        await olderUnmounted();

        await new Promise(resolve => setTimeout(resolve, 50));
        resolved('scopes').should.equal('shared');
        resolved('query.Shared.value').should.equal('"newer"');
    });

    it('should withdraw it when the newer publisher unmounts too', async () => {
        const view = await olderUnmounted();

        view.rerenderUnderProvider(probe);

        await waitFor(() => resolved('scopes').should.equal(''));
    });
});
