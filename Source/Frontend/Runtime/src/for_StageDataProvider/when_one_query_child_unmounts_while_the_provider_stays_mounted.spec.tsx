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

const paths = ['query.Orders', 'query.Products'];

describe('when one query child unmounts while the provider stays mounted', () => {
    async function mounted() {
        const view = renderUnderProvider(<>
            <Publisher key='orders' scope='orders' name='Orders' alias='Basket' list value='order' />
            <Publisher key='products' scope='products' name='Products' value='product' />
            <Probe paths={paths} />
        </>);
        await waitFor(() => resolved('scopes').should.equal('orders,products'));
        resolved('results').should.include('Basket');
        return view;
    }

    it('should withdraw its registration', async () => {
        const view = await mounted();

        view.rerenderUnderProvider(<><Publisher key='products' scope='products' name='Products' value='product' /><Probe paths={paths} /></>);

        await waitFor(() => resolved('scopes').should.equal('products'));
    });

    it('should no longer resolve it by its name, alias or unnamed path', async () => {
        const view = await mounted();

        view.rerenderUnderProvider(<><Publisher key='products' scope='products' name='Products' value='product' /><Probe paths={paths} /></>);

        await waitFor(() => resolved('query.Orders').should.equal('null'));
        resolved('results').should.not.include('Basket');
    });

    it('should leave an unrelated publisher resolving', async () => {
        const view = await mounted();

        view.rerenderUnderProvider(<><Publisher key='products' scope='products' name='Products' value='product' /><Probe paths={paths} /></>);

        await waitFor(() => resolved('scopes').should.equal('products'));
        resolved('query.Products').should.equal('[{"value":"product"}]');
    });
});
