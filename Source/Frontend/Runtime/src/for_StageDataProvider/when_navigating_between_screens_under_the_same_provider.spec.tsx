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

const paths = ['query.Orders', 'query.Basket', 'value', 'query.Invoices'];

const ordersScreen = <>
    <Publisher key='orders' scope='orders' name='Orders' alias='query.Basket' list value='order' />
    <Publisher key='unnamed' scope='unnamed' value='row' />
    <Probe paths={paths} />
</>;
const invoicesScreen = <>
    <Publisher key='invoices' scope='invoices' name='Invoices' value='invoice' />
    <Probe paths={paths} />
</>;

describe('when navigating between screens under the same provider', () => {
    async function navigated() {
        const view = renderUnderProvider(ordersScreen);
        await waitFor(() => resolved('scopes').should.equal('orders,unnamed'));
        view.rerenderUnderProvider(invoicesScreen);
        await waitFor(() => resolved('scopes').should.equal('invoices'));
    }

    it('should no longer resolve the previous screen\'s query name or alias', async () => {
        await navigated();

        resolved('query.Orders').should.equal('null');
        resolved('query.Basket').should.equal('null');
    });

    it('should no longer resolve the previous screen\'s unnamed binding', async () => {
        await navigated();

        await waitFor(() => resolved('value').should.equal('"invoice"'));
    });

    it('should resolve the new screen\'s query', async () => {
        await navigated();

        await waitFor(() => resolved('query.Invoices').should.equal('[{"value":"invoice"}]'));
    });
});
