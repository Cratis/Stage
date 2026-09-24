// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render as renderWithoutProvider, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ExternalComponent } from '@cratis/scene.model';
import { PrimeReactProvider } from '@primereact/core';
import { StageAction, StageTable } from './stageComponents';
import { element } from './testElements';

// PrimeReact 11's components read their configuration from a `PrimeReactProvider` up the tree - without
// one they throw rather than render unstyled, so every spec needs one even though none of them assert
// anything about theming.
function render(ui: ReactElement) {
    return renderWithoutProvider(<PrimeReactProvider>{ui}</PrimeReactProvider>);
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
});
