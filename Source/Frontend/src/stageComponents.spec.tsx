// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render as renderWithoutProvider, screen, waitFor } from '@testing-library/react';
import { useEffect, useState } from 'react';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ExternalComponent } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core';
import { StageAction, StageCommandForm, StageTable } from './stageComponents';
import { dataChanged, StageDataProvider, useStageData, useStageQuery } from './stageData';
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

function ComponentOutput({ name, value }: { name: string; value: unknown }) {
    const { setState } = useStageData();
    useEffect(() => { setState(name, value); }, []);
    return null;
}

function commandResult(overrides: Partial<Record<string, unknown>> = {}) {
    return {
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: null,
        ...overrides,
    };
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
            <BindingProbe label='data' path={{ kind: 'dataContext', path: 'invoiceNumber' }} />
            <BindingProbe label='query' path={{ kind: 'queryResult', query: 'AllInvoices', path: 'invoiceNumber' }} />
        </>);
        fireEvent.click((await screen.findByText('INV-1')).closest('tr')!);

        expect(screen.getByLabelText('data').textContent).toEqual('INV-1');
        expect(screen.getByLabelText('query').textContent).toEqual('INV-1');
    });

    it('uses the shared typed resolver for nested component paths, null behavior and diagnostics', async () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
        renderWithData(<>
            <ComponentOutput name='editor.value.text' value='Nested title' />
            <BindingProbe label='component' path={{ kind: 'componentProperty', componentId: 'editor', componentPropertyPath: 'value.text' }} />
            <BindingProbe label='preserve' path={{ kind: 'dataContext', path: 'missing', nullBehavior: 'preserve' }} />
            <BindingProbe label='type' path={{ kind: 'literal', path: '', value: 'not a number', expectedValueType: 'number' }} />
            <BindingProbe label='mode' path={{ kind: 'dataContext', path: 'missing', mode: 'twoWay' }} />
        </>);

        await waitFor(() => expect(screen.getByLabelText('component').textContent).toEqual('Nested title'));
        expect(screen.getByLabelText('preserve').textContent).toEqual('');
        expect(warn.mock.calls.map(call => String(call[0])).join('\n')).toContain('bindingTypeMismatch');
        expect(warn.mock.calls.map(call => String(call[0])).join('\n')).toContain('unsupportedTwoWayBinding');
        warn.mockRestore();
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
    it('keeps invalid required fields inside the native form boundary', async () => {
        const fetched = vi.fn();
        vi.stubGlobal('fetch', fetched);
        const form = element('order-form', 'Stage:commandForm', {
            command: 'RegisterOrder',
            label: 'Register order',
            fields: [{ name: 'orderNumber', label: 'Order #' }],
        });

        renderWithoutProvider(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RegisterOrder: '/api/orders/register' }, queries: {} }} locale='en' locales={['en']} screen='Orders'>
                    <StageCommandForm element={form} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        fireEvent.click(screen.getByRole('button', { name: 'Execute Register order' }));

        await waitFor(() => expect(screen.getByLabelText('Order #').getAttribute('aria-invalid')).toEqual('true'));
        expect(fetched).not.toHaveBeenCalled();
    });

    it('generates rich fields automatically and honors manually placed unequal columns', () => {
        const form = element('profile-form', 'Stage:commandForm', {
            command: 'RegisterProfile',
            label: 'Register profile',
            mode: 'auto',
            geometry: {
                mode: 'manual',
                columns: [
                    { width: '12rem', resizable: true, fields: [{ name: 'name', width: '10rem' }, 'enabled'] },
                    { grow: 2, fields: [{ name: 'amount', width: '9rem' }, 'dueDate'] },
                ],
            },
            schema: JSON.stringify({
                required: ['name', 'enabled', 'amount', 'dueDate'],
                properties: {
                    name: { type: 'string' },
                    enabled: { type: 'boolean' },
                    amount: { type: 'number' },
                    dueDate: { type: 'string', format: 'date' },
                },
            }),
        });

        const { container } = renderWithoutProvider(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RegisterProfile: '/api/profiles/register' }, queries: {} }} locale='en' locales={['en']} screen='Profiles'>
                    <StageCommandForm element={form} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );

        expect(screen.getByLabelText('name')).toBeDefined();
        expect(screen.getByLabelText('enabled')).toBeDefined();
        expect(screen.getByLabelText('amount')).toBeDefined();
        expect(screen.getByLabelText('dueDate')).toBeDefined();
        const layout = container.querySelector('.stage-command-form__layout') as HTMLElement;
        const columns = [...container.querySelectorAll('.stage-command-form__column')] as HTMLElement[];
        const amount = container.querySelector('[data-field="amount"]') as HTMLElement;
        expect(layout.dataset.layoutMode).toEqual('manual');
        expect(layout.style.gridTemplateColumns).toContain('12rem');
        expect(layout.style.gridTemplateColumns).toContain('2fr');
        expect(columns[0].dataset.resizable).toEqual('true');
        expect(amount.style.width).toEqual('9rem');
    });

    it('keeps dirty field values when a query refresh updates Stage data', async () => {
        let reads = 0;
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'api/orders') {
                reads += 1;
                return Promise.resolve({ ok: true, json: async () => ({ data: [{ id: String(reads), orderNumber: `O-${reads}` }] }) });
            }

            return Promise.resolve({ ok: true, json: async () => commandResult() });
        }));
        const table = element('orders', 'core:table', { route: '/api/orders', typeName: 'Orders', dataKey: 'id' }, {
            columns: [element('order-number', 'core:column', { property: 'orderNumber', label: 'Order' })],
        });
        const form = element('order-form', 'Stage:commandForm', {
            command: 'RegisterOrder',
            label: 'Register order',
            fields: [{ name: 'orderNumber', label: 'Order #' }],
        });

        renderWithoutProvider(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RegisterOrder: '/api/orders/register' }, queries: {} }} locale='en' locales={['en']} screen='Orders'>
                    <StageTable element={table} slots={{}} />
                    <StageCommandForm element={form} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        await screen.findByText('O-1');
        fireEvent.change(screen.getByLabelText('Order #'), { target: { value: 'O-99' } });

        dataChanged();

        expect(await screen.findByText('O-2')).toBeDefined();
        expect((screen.getByLabelText('Order #') as HTMLInputElement).value).toEqual('O-99');
    });

    it('uses form metadata, command route lookup and validation feedback', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult({ isSuccess: false, isValid: false, validationResults: [{ severity: 0, message: 'Order number is required', members: ['orderNumber'] }] }) });
        vi.stubGlobal('fetch', fetched);
        const form = element('order-form', 'Stage:commandForm', {
            command: 'RegisterOrder',
            label: 'Register order',
            fields: [{ name: 'orderNumber', label: 'Order #' }],
        });

        renderWithoutProvider(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RegisterOrder: '/api/orders/register' }, queries: {} }} locale='en' locales={['en']} screen='Orders'>
                    <StageCommandForm element={form} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        fireEvent.change(screen.getByLabelText('Order #'), { target: { value: 'O-1' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Register order' }));

        await waitFor(() => expect(fetched).toHaveBeenCalled());
        expect(String(fetched.mock.calls[0][0])).toContain('/api/orders/register');
        expect(fetched.mock.calls[0][1]).toEqual(expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ orderNumber: 'O-1' }),
        }));
        expect((await screen.findAllByText('Order number is required')).length).toBeGreaterThan(0);
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
        const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetchMock);

        render(<StageAction element={action} slots={{}} />);
        fireEvent.click(screen.getByRole('button', { name: 'Register invoice' }));
        fireEvent.change(screen.getByLabelText('invoiceNumber'), { target: { value: 'INV-7' } });
        fireEvent.change(screen.getByLabelText('amount'), { target: { value: '13' } });
        fireEvent.blur(screen.getByLabelText('amount'));
        fireEvent.click(screen.getByRole('button', { name: /Execute/ }));

        await waitFor(() => expect(fetchMock).toHaveBeenCalled());
        expect(String(fetchMock.mock.calls[0][0])).toContain('/api/sales/invoices/register-invoice');
        expect(fetchMock.mock.calls[0][1]).toEqual(expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ invoiceNumber: 'INV-7', amount: 13 }),
        }));
    });

    it('shows the validation the command rejected with', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: async () => commandResult({ isSuccess: false, isValid: false, validationResults: [{ severity: 0, message: 'Invoice number is required', members: ['invoiceNumber'] }] }),
        }));

        render(<StageAction element={action} slots={{}} />);
        fireEvent.click(screen.getByRole('button', { name: 'Register invoice' }));
        fireEvent.change(screen.getByLabelText('invoiceNumber'), { target: { value: 'INV-7' } });
        fireEvent.change(screen.getByLabelText('amount'), { target: { value: '13' } });
        fireEvent.click(screen.getByRole('button', { name: /Execute/ }));

        expect((await screen.findAllByText('Invoice number is required')).length).toBeGreaterThan(0);
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

            if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => commandResult() });
            return Promise.resolve({ ok: false, json: async () => ({}) });
        });
        vi.stubGlobal('fetch', fetched);
        const table = element('invoices', 'core:table', { route: '/api/sales/invoices/all-invoices', typeName: 'Invoice', dataKey: 'id' }, {
            columns: [element('invoice-number', 'core:column', { property: 'invoiceNumber', label: 'Invoice' })],
        });

        renderWithData(<><StageTable element={table} slots={{}} /><StageAction element={action} slots={{}} /></>);
        expect(await screen.findByText('INV-1')).toBeDefined();
        fireEvent.click(screen.getByRole('button', { name: 'Register invoice' }));
        fireEvent.change(screen.getByLabelText('invoiceNumber'), { target: { value: 'INV-8' } });
        fireEvent.change(screen.getByLabelText('amount'), { target: { value: '14' } });
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
