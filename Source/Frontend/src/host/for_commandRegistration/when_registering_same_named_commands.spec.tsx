// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registeredCommand } from '../../commandExecutor';
import { StageCommandForm } from '../../StageCommandForm';
import { element } from '../../testElements';
import { commandResult } from '../../testing/commands';
import { registerStageCommands } from '../commandRegistration';

const should = chai.should();
describe('when registering same-named commands in different modules', () => {
    afterEach(() => vi.unstubAllGlobals());
    it('should execute each form against its own route with its own schema without a simple-name alias', async () => {
        const billing = 'Stage.Billing.Registration.Add.Register';
        const inventory = 'Stage.Inventory.Registration.Add.Register';
        registerStageCommands({ commands: { [billing]: '/api/billing/register', [inventory]: '/api/inventory/register' }, queries: {}, commandSchemas: {
            [billing]: JSON.stringify({ properties: { invoiceNumber: { type: 'string' } }, required: ['invoiceNumber'] }),
            [inventory]: JSON.stringify({ properties: { quantity: { type: 'integer' } }, required: ['quantity'] }),
        } });
        const requests: { url: string; body: unknown }[] = [];
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
            requests.push({ url: String(input), body: JSON.parse(String(init?.body)) });
            return new Response(JSON.stringify(commandResult()));
        }));
        const billingForm = element('billing', 'Stage:commandForm', { command: 'Register', typeName: billing, route: '/api/billing/register', label: 'Invoice', fields: [{ name: 'invoiceNumber', label: 'Invoice number' }] });
        // A planned route also disambiguates an element that does not carry its full type name.
        const inventoryForm = element('inventory', 'Stage:commandForm', { command: 'Register', route: '/api/inventory/register', label: 'Stock', fields: [{ name: 'quantity', label: 'Quantity' }], schema: JSON.stringify({ properties: { quantity: { type: 'integer' } }, required: ['quantity'] }) });
        render(<PrimeReactProvider><StageCommandForm element={billingForm} /><StageCommandForm element={inventoryForm} /></PrimeReactProvider>);
        fireEvent.change(screen.getByLabelText('Invoice number'), { target: { value: 'INV-1' } });
        fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '4' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Invoice' }));
        fireEvent.click(screen.getByRole('button', { name: 'Execute Stock' }));
        await waitFor(() => requests.should.have.lengthOf(2));
        requests.should.deep.equal([
            { url: 'http://localhost:3000/api/billing/register', body: { invoiceNumber: 'INV-1' } },
            { url: 'http://localhost:3000/api/inventory/register', body: { quantity: 4 } },
        ]);
        should.equal(registeredCommand('Register'), undefined);
    });
});
