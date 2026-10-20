// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm as StageCommandForm } from './testing/commands';
import { commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { schemaPropertiesOf } from './stageFormSchema';
import { element } from './testElements';

const address = {
    type: 'object',
    required: ['street', 'city'],
    properties: { street: { type: 'string' }, city: { type: 'string' }, country: { type: 'string' } },
};
const line = { type: 'object', required: ['sku', 'quantity'], properties: { sku: { type: 'string' }, quantity: { type: 'integer' } } };
const schema = JSON.stringify({
    type: 'object',
    required: ['basketId', 'shipTo', 'lines'],
    properties: {
        basketId: { type: 'string', format: 'uuid', 'x-concept': 'BasketId' },
        shipTo: address,
        method: { type: 'string', enum: ['standard', 'express', 'pickup'], 'x-concept': 'DeliveryMethod' },
        lines: { type: 'array', items: line },
        tags: { type: 'array', items: { type: 'string' } },
    },
});
const routes = { commands: { CheckOut: '/api/checkout' }, queries: { GetCheckoutDraft: '/api/checkout/draft' } };

function form(properties: Record<string, unknown> = {}) {
    return element('checkout', 'Stage:commandForm', {
        command: 'CheckOut', label: 'Check out', schema, generationMode: FormGenerationMode.Auto,
        fields: [{ name: 'shipTo', label: 'Deliver to' }, { name: 'method', label: 'Delivery' }, { name: 'lines', label: 'Lines' }],
        ...properties,
    });
}

function view(properties: Record<string, unknown> = {}) {
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Checkout'>
                <StageCommandForm element={form(properties)} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}

function submitted(fetched: ReturnType<typeof vi.fn>) {
    const call = fetched.mock.calls.find(([url]) => String(url).includes('/api/checkout') && !String(url).includes('draft'));
    return call ? JSON.parse(String((call[1] as RequestInit).body)) : undefined;
}

describe('a command form for a command with enumeration, record and collection members', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); globalThis.localStorage?.clear(); });

    it('reads the members of a record, the element of a collection and the values of an enumeration from the schema', () => {
        const [, shipTo, method, lines, tags] = schemaPropertiesOf(schema);

        expect(shipTo.members?.map(member => [member.name, member.required])).toEqual([['street', true], ['city', true], ['country', false]]);
        expect(method.options).toEqual(['standard', 'express', 'pickup']);
        expect(lines.item?.members?.map(member => member.name)).toEqual(['sku', 'quantity']);
        expect(lines.item?.members?.[1].type).toBe('number');
        expect(tags.item?.type).toBe('text');
        expect([shipTo, method, lines, tags].some(property => property.unsupportedReason)).toBe(false);
    });

    it('presents a record as a group of its members and an enumeration as a choice among its values', () => {
        view();

        const group = screen.getByRole('group', { name: 'Deliver to' });
        expect(within(group).getByLabelText('Street')).toBeDefined();
        expect(within(group).getByLabelText('City')).toBeDefined();
        expect(within(group).getByLabelText('Country')).toBeDefined();
        expect(screen.getByText('Delivery')).toBeDefined();
        expect(screen.getByRole('group', { name: 'Lines' })).toBeDefined();
    });

    it('submits the record and the list the shopper filled in, and the identity the form does not show', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        view({ fields: [{ name: 'shipTo', label: 'Deliver to' }, { name: 'lines', label: 'Lines' }], generationMode: FormGenerationMode.Manual });

        const group = screen.getByRole('group', { name: 'Deliver to' });
        fireEvent.change(within(group).getByLabelText('Street'), { target: { value: '12 Analytical Way' } });
        fireEvent.change(within(group).getByLabelText('City'), { target: { value: 'Oslo' } });
        fireEvent.click(screen.getByRole('button', { name: 'Add Lines' }));
        const lines = screen.getByRole('group', { name: 'Lines' });
        fireEvent.change(within(lines).getByLabelText('Sku'), { target: { value: 'ACM-1000' } });
        fireEvent.change(within(lines).getByLabelText('Quantity'), { target: { value: '2' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Check out' }));

        await waitFor(() => expect(submitted(fetched)).toBeDefined());
        const body = submitted(fetched);
        expect(body.shipTo).toEqual({ street: '12 Analytical Way', city: 'Oslo' });
        expect(body.lines).toEqual([{ sku: 'ACM-1000', quantity: 2 }]);
        expect(body.basketId).toMatch(/^[0-9a-f-]{36}$/);
    });

    it('submits the name of the value chosen from an enumeration', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        view({
            fields: [{ name: 'method', label: 'Delivery' }],
            generationMode: FormGenerationMode.Manual,
            schema: JSON.stringify({ type: 'object', required: ['method'], properties: { method: { type: 'string', enum: ['standard', 'express', 'pickup'] } } }),
        });

        const choice = document.querySelector('.stage-command-form__field select') as HTMLSelectElement;
        expect([...choice.options].map(option => option.text)).toEqual(expect.arrayContaining(['Standard', 'Express', 'Pickup']));
        fireEvent.change(choice, { target: { value: [...choice.options].find(option => option.text === 'Express')?.value } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Check out' }));

        await waitFor(() => expect(submitted(fetched)).toBeDefined());
        expect(submitted(fetched).method).toBe('express');
    });

    it('removes an element of a collection', () => {
        view();

        fireEvent.click(screen.getByRole('button', { name: 'Add Lines' }));
        fireEvent.click(screen.getByRole('button', { name: 'Add Lines' }));
        expect(screen.getAllByRole('button', { name: /^Remove Lines/ })).toHaveLength(2);
        fireEvent.click(screen.getByRole('button', { name: 'Remove Lines 1' }));

        expect(screen.getAllByRole('button', { name: /^Remove Lines/ })).toHaveLength(1);
    });

    it('does not submit a record that is missing a member the schema requires', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        view({ fields: [{ name: 'shipTo', label: 'Deliver to' }], generationMode: FormGenerationMode.Manual });

        fireEvent.change(within(screen.getByRole('group', { name: 'Deliver to' })).getByLabelText('Street'), { target: { value: '12 Analytical Way' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Check out' }));

        expect((await screen.findAllByText('Deliver to: City is required')).length).toBeGreaterThan(0);
        expect(submitted(fetched)).toBeUndefined();
    });

    it('starts from what the query it populates via answered, the members the form does not list included', async () => {
        const draft = { shipTo: { street: '12 Analytical Way', city: 'Oslo' }, method: 1, lines: [{ sku: 'ACM-1000', quantity: 2 }] };
        const fetched = vi.fn((url: RequestInfo | URL) => Promise.resolve(String(url).includes('draft')
            ? { ok: true, json: async () => [draft] }
            : { ok: true, json: async () => commandResult() }));
        vi.stubGlobal('fetch', fetched);
        view({
            fields: [{ name: 'shipTo', label: 'Deliver to' }],
            generationMode: FormGenerationMode.Manual,
            populateSource: { queryName: 'GetCheckoutDraft', parameterBindings: { basketId: { path: 'basketId' } } },
        });

        await waitFor(() => expect((within(screen.getByRole('group', { name: 'Deliver to' })).getByLabelText('Street') as HTMLInputElement).value).toBe('12 Analytical Way'));
        expect(String(fetched.mock.calls.find(([url]) => String(url).includes('draft'))?.[0])).toMatch(/draft\?basketId=[0-9a-f-]{36}/);
        fireEvent.click(screen.getByRole('button', { name: 'Execute Check out' }));

        await waitFor(() => expect(submitted(fetched)).toBeDefined());
        const body = submitted(fetched);
        expect(body.shipTo).toEqual({ street: '12 Analytical Way', city: 'Oslo' });
        expect(body.method).toBe('express');
        expect(body.lines).toEqual([{ sku: 'ACM-1000', quantity: 2 }]);
        const queried = String(fetched.mock.calls.find(([url]) => String(url).includes('draft'))?.[0]).split('basketId=')[1];
        expect(body.basketId).toBe(queried);
    });
});
