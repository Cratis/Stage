// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render as renderWithoutProvider, screen, waitFor } from '@testing-library/react';
import { useState } from 'react';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ExternalComponent } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core';
import { StageAction, StageCommandForm, StageTable } from './stageComponents';
import { StageDataProvider, useStageData, useStageQuery } from './stageData';
import { element } from './testElements';

// PrimeReact 11's components read their configuration from a `PrimeReactProvider` up the tree - without
// one they throw rather than render unstyled, so every spec needs one even though none of them assert
// anything about theming.
function render(ui: ReactElement) {
    return renderWithoutProvider(<PrimeReactProvider>{ui}</PrimeReactProvider>);
}

function renderWithData(ui: ReactElement) {
    return renderWithoutProvider(
        <PrimeReactProvider>
            <StageDataProvider routes={undefined} locale='en' locales={['en']} screen='Invoices'>{ui}</StageDataProvider>
        </PrimeReactProvider>,
    );
}

function BindingProbe({ path, label = 'selection' }: { path: string | Record<string, unknown>; label?: string }) {
    const value = useStageData().resolveBinding(path as never);
    return <output aria-label={label}>{value === undefined ? '' : String(value)}</output>;
}

describe('a synthesized table', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({ data: [{ invoiceNumber: 'INV-1', amount: 42 }] }),
        }));
    });

    it('reads the modeled query and shows its rows', async () => {
        const table = element('table', 'core:table', { route: '/api/sales/invoices/all-invoices', typeName: 'Invoice' }, {
            columns: [
                element('c1', 'core:column', { property: 'invoiceNumber', label: 'Invoice #' }),
                element('c2', 'core:column', { property: 'amount', label: 'Amount' }),
            ],
        });

        render(<StageTable element={table} slots={{}} />);

        expect(await screen.findByText('INV-1')).toBeDefined();
        expect(screen.getByText('42')).toBeDefined();
        expect(fetch).toHaveBeenCalledWith('api/sales/invoices/all-invoices', expect.anything());
    });

    it('shows the columns the rows carry when the model declares none', async () => {
        const table = element('table', 'core:table', { route: '/api/sales/invoices/all-invoices', typeName: 'Invoice' });
        render(<StageTable element={table} slots={{}} />);

        expect(await screen.findByText('invoiceNumber')).toBeDefined();
        expect(screen.getByText('INV-1')).toBeDefined();
    });

    it('says so when the model exposes no query', () => {
        const table = element('table', 'core:table', { typeName: 'Invoice' }) as ExternalComponent;
        render(<StageTable element={table} slots={{}} />);
        expect(screen.getByText(/No query is exposed/)).toBeDefined();
    });

    it('binds details to the selected row and clears the scoped selection', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({ data: [{ id: '1', invoiceNumber: 'INV-1' }, { id: '2', invoiceNumber: 'INV-2' }] }),
        }));
        const table = element('invoices', 'core:table', { route: '/api/sales/invoices/all-invoices', typeName: 'Invoice', dataKey: 'id' }, {
            columns: [element('c1', 'core:column', { property: 'invoiceNumber', label: 'Invoice #' })],
        });

        renderWithData(<><StageTable element={table} slots={{}} /><BindingProbe path='data.invoices.invoiceNumber' /></>);
        const second = (await screen.findByText('INV-2')).closest('tr')!;

        fireEvent.click(second);
        expect(screen.getByLabelText('selection').textContent).toEqual('INV-2');
        fireEvent.click(screen.getByRole('button', { name: 'Clear selection' }));
        expect(screen.getByLabelText('selection').textContent).toEqual('');
        fireEvent.keyDown(screen.getByText('INV-1').closest('tr')!, { key: 'Enter' });
        expect(screen.getByLabelText('selection').textContent).toEqual('INV-1');
    });

    it('keeps selection scoped to independent table instances', async () => {
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'api/customers') return Promise.resolve({ ok: true, json: async () => ({ data: [{ id: 'C1', name: 'Customer one' }, { id: 'C2', name: 'Customer two' }] }) });
            return Promise.resolve({ ok: true, json: async () => ({ data: [{ id: 'P1', name: 'Product one' }, { id: 'P2', name: 'Product two' }] }) });
        }));
        const customers = element('customers', 'core:table', { route: '/api/customers', typeName: 'Customers', dataKey: 'id' }, {
            columns: [element('customer-name', 'core:column', { property: 'name', label: 'Customer' })],
        });
        const products = element('products', 'core:table', { route: '/api/products', typeName: 'Products', dataKey: 'id' }, {
            columns: [element('product-name', 'core:column', { property: 'name', label: 'Product' })],
        });

        renderWithData(<><StageTable element={customers} slots={{}} /><StageTable element={products} slots={{}} /><BindingProbe label='customer' path='data.customers.name' /><BindingProbe label='product' path='data.products.name' /></>);
        fireEvent.click((await screen.findByText('Customer two')).closest('tr')!);
        fireEvent.click((await screen.findByText('Product one')).closest('tr')!);

        expect(screen.getByLabelText('customer').textContent).toEqual('Customer two');
        expect(screen.getByLabelText('product').textContent).toEqual('Product one');
        fireEvent.click(screen.getAllByRole('button', { name: 'Clear selection' })[0]);
        expect(screen.getByLabelText('customer').textContent).toEqual('');
        expect(screen.getByLabelText('product').textContent).toEqual('Product one');
    });

    it('resolves typed data context and query result bindings', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({ data: [{ id: '1', invoiceNumber: 'INV-1' }] }),
        }));
        const table = element('invoices', 'core:table', { route: '/api/sales/invoices/all-invoices', query: 'AllInvoices', typeName: 'Invoice', dataKey: 'id' }, {
            columns: [element('invoice-number', 'core:column', { property: 'invoiceNumber', label: 'Invoice' })],
        });

        renderWithData(<>
            <StageTable element={table} slots={{}} />
            <BindingProbe label='data' path={{ kind: 'dataContext', path: 'invoices.invoiceNumber' }} />
            <BindingProbe label='query' path={{ kind: 'queryResult', query: 'AllInvoices', path: 'invoiceNumber' }} />
        </>);
        fireEvent.click((await screen.findByText('INV-1')).closest('tr')!);

        expect(screen.getByLabelText('data').textContent).toEqual('INV-1');
        expect(screen.getByLabelText('query').textContent).toEqual('INV-1');
    });

    it('rebinds query arguments from selection without leaking stale arguments', async () => {
        const fetched = vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'api/customers') return Promise.resolve({ ok: true, json: async () => ({ data: [{ id: 'C1', name: 'Customer one' }, { id: 'C2', name: 'Customer two' }] }) });
            return Promise.resolve({ ok: true, json: async () => ({ data: [] }) });
        });
        vi.stubGlobal('fetch', fetched);
        const customers = element('customers', 'core:table', { route: '/api/customers', typeName: 'Customers', dataKey: 'id' }, {
            columns: [element('customer-name', 'core:column', { property: 'name', label: 'Customer' })],
        });
        const invoices = element('invoices', 'core:table', { route: '/api/invoices', query: 'InvoicesForCustomer', typeName: 'Invoices', queryArguments: { customerId: { path: 'data.customers.id' } } }, {
            columns: [element('invoice-number', 'core:column', { property: 'invoiceNumber', label: 'Invoice' })],
        });

        renderWithData(<><StageTable element={customers} slots={{}} /><StageTable element={invoices} slots={{}} /></>);
        fireEvent.click((await screen.findByText('Customer two')).closest('tr')!);

        await waitFor(() => expect(fetched).toHaveBeenCalledWith('api/invoices?customerId=C2', expect.anything()));
    });
});

describe('a native command form', () => {
    it('uses form metadata, command route lookup and validation feedback', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ validationResults: [{ message: 'Order number is required' }] }) });
        vi.stubGlobal('fetch', fetched);
        const form = element('order-form', 'Stage:commandForm', {
            command: 'RegisterOrder',
            label: 'Register order',
            fields: [{ name: 'orderNumber', label: 'Order #' }],
        });

        renderWithoutProvider(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RegisterOrder: '/api/orders/register' }, queries: {} }} locale='en' locales={['en']} screen='Orders'>
                    <StageCommandForm element={form} slots={{}} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        fireEvent.change(screen.getByLabelText('Order #'), { target: { value: 'O-1' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Register order' }));

        await waitFor(() => expect(fetched).toHaveBeenCalledWith('api/orders/register', expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ orderNumber: 'O-1' }),
        })));
        expect(await screen.findByText('Order number is required')).toBeDefined();
    });
});

describe('a synthesized action', () => {
    const action = element('action', 'core:action', {
        command: 'RegisterInvoice',
        label: 'Register invoice',
        route: '/api/sales/invoices/register-invoice',
        schema: JSON.stringify({ properties: { invoiceNumber: { type: 'string' }, amount: { type: 'number' } } }),
    });

    it('posts the modeled command with the values entered', async () => {
        const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isSuccess: true }) });
        vi.stubGlobal('fetch', fetchMock);

        render(<StageAction element={action} slots={{}} />);
        fireEvent.click(screen.getByRole('button', { name: 'Register invoice' }));
        fireEvent.change(screen.getByLabelText('invoiceNumber'), { target: { value: 'INV-7' } });
        fireEvent.change(screen.getByLabelText('amount'), { target: { value: '13' } });
        fireEvent.blur(screen.getByLabelText('amount'));
        fireEvent.click(screen.getByRole('button', { name: /Execute/ }));

        await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('api/sales/invoices/register-invoice', expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ invoiceNumber: 'INV-7', amount: 13 }),
        })));
    });

    it('shows the validation the command rejected with', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({ validationResults: [{ message: 'Invoice number is required' }] }),
        }));

        render(<StageAction element={action} slots={{}} />);
        fireEvent.click(screen.getByRole('button', { name: 'Register invoice' }));
        fireEvent.click(screen.getByRole('button', { name: /Execute/ }));

        expect(await screen.findByText('Invoice number is required')).toBeDefined();
    });

    it('refreshes scoped queries after a successful command', async () => {
        let queryReads = 0;
        const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
            const url = String(input);
            if (url === 'api/sales/invoices/all-invoices') {
                queryReads += 1;
                const invoiceNumber = queryReads === 1 ? 'INV-1' : 'INV-2';
                return Promise.resolve({ ok: true, json: async () => ({ data: [{ id: invoiceNumber, invoiceNumber }] }) });
            }

            if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => ({ isSuccess: true }) });
            return Promise.resolve({ ok: false, json: async () => ({}) });
        });
        vi.stubGlobal('fetch', fetched);
        const table = element('invoices', 'core:table', { route: '/api/sales/invoices/all-invoices', typeName: 'Invoice', dataKey: 'id' }, {
            columns: [element('invoice-number', 'core:column', { property: 'invoiceNumber', label: 'Invoice' })],
        });

        renderWithData(<><StageTable element={table} slots={{}} /><StageAction element={action} slots={{}} /></>);
        expect(await screen.findByText('INV-1')).toBeDefined();
        fireEvent.click(screen.getByRole('button', { name: 'Register invoice' }));
        fireEvent.click(screen.getByRole('button', { name: /Execute/ }));

        expect(await screen.findByText('INV-2')).toBeDefined();
    });
});

describe('a scoped query lifecycle', () => {
    function QueryProbe({ customerId }: { customerId: string }) {
        const query = useStageQuery({ scope: 'details', name: 'Details', route: '/api/details', arguments: { customerId } });
        return <output aria-label='query-result'>{String(query.rows[0]?.invoiceNumber ?? '')}</output>;
    }

    function RebindingQuery() {
        const [customerId, setCustomerId] = useState('C1');
        return (
            <>
                <button type='button' onClick={() => setCustomerId('C2')}>Select C2</button>
                <QueryProbe customerId={customerId} />
            </>
        );
    }

    it('aborts stale requests and keeps the newest result', async () => {
        let resolveFirst: ((value: Response) => void) | undefined;
        const signals: AbortSignal[] = [];
        const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
            signals.push(init?.signal as AbortSignal);
            const url = String(input);
            if (url === 'api/details?customerId=C1') {
                return new Promise<Response>(resolve => { resolveFirst = resolve; });
            }

            return Promise.resolve({ ok: true, json: async () => ({ data: [{ invoiceNumber: 'INV-C2' }] }) } as Response);
        });
        vi.stubGlobal('fetch', fetched);

        renderWithData(<RebindingQuery />);
        fireEvent.click(screen.getByRole('button', { name: 'Select C2' }));
        expect(await screen.findByText('INV-C2')).toBeDefined();
        expect(signals[0].aborted).toBe(true);
        resolveFirst?.({ ok: true, json: async () => ({ data: [{ invoiceNumber: 'INV-C1' }] }) } as Response);
        await waitFor(() => expect(screen.getByLabelText('query-result').textContent).toEqual('INV-C2'));
    });
});
