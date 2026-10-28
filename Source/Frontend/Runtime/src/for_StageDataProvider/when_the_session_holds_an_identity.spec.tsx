// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { StageDataProvider, useStageData } from '../stageData';
import { element } from '../testElements';
import { commandResult, RegisteredForm } from '../testing/commands';

chai.should();

const text = (label: string) => screen.getByLabelText(label).textContent ?? '';
const addToBasket = JSON.stringify({
    type: 'object',
    required: ['basketId', 'sku'],
    properties: {
        basketId: { type: 'string', format: 'uuid', 'x-concept': 'BasketId' },
        sku: { type: 'string' },
    },
});
const routes = { commands: { AddToBasket: '/api/basket/add' }, queries: {} };

function DataProbe() {
    const data = useStageData();
    return (
        <>
            <output aria-label='parameters'>{JSON.stringify(data.parameters)}</output>
            <output aria-label='session'>{JSON.stringify(data.session)}</output>
            <output aria-label='key'>{String(data.resolveBinding({ path: 'screen.keys.basketId' }) ?? '')}</output>
        </>
    );
}

function view(parameters: Record<string, string> = { productId: 'p1' }) {
    const form = element('add', 'Stage:commandForm', {
        command: 'AddToBasket', label: 'Add', schema: addToBasket, generationMode: FormGenerationMode.Manual,
        fields: [{ name: 'sku', label: 'Product' }],
    });
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={parameters} locale='en' locales={['en']} screen='Products'>
                <DataProbe />
                <RegisteredForm element={form} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}

function submitted(fetched: ReturnType<typeof vi.fn>): Record<string, unknown> | undefined {
    const call = fetched.mock.calls.find(([url]) => String(url).includes('/api/basket/add'));
    return call ? JSON.parse(String((call[1] as RequestInit).body)) as Record<string, unknown> : undefined;
}

describe('when the session holds an identity', () => {
    beforeEach(() => globalThis.localStorage.setItem('stage.session.BasketId', 'b1'));
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('should keep it out of the parameters of a screen whose address does not carry it', () => {
        view();

        JSON.parse(text('parameters')).should.deep.equal({ productId: 'p1' });
    });

    it('should offer it as the session scope', () => {
        view();

        JSON.parse(text('session')).should.deep.equal({ basketId: 'b1' });
    });

    it('should offer it as the key a data declaration is read by when the address does not carry it', () => {
        view();

        text('key').should.equal('b1');
    });

    it('should let the address win as the key a data declaration is read by', () => {
        view({ basketId: 'b2' });

        text('key').should.equal('b2');
    });

    it('should still populate it into a form that does not show it', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        view();

        fireEvent.change(screen.getByLabelText('Product'), { target: { value: 'ACM-1000' } });
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));

        await waitFor(() => (submitted(fetched) !== undefined).should.equal(true));
        (submitted(fetched) ?? {}).should.include({ basketId: 'b1', sku: 'ACM-1000' });
    });
});
