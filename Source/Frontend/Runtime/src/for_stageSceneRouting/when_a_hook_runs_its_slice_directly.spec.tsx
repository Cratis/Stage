// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen, waitFor, within } from '../testing/render';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OutletItemKind, OutletKind, Opens, OutletSubject, type ExternalComponent } from '@cratis/scene.model';
import { SceneOutletsProvider, SliceInvokerProvider, type RegisteredComponentProps } from '@cratis/scene.react';
import type { ComponentType } from 'react';
import { InputSourceKind } from '@cratis/scene.engine';
import { stageComponents } from '../stageComponents';
import { createStageSliceInvoker } from '../stageSlices';
import { stageActionSlicesOf } from '../stageOutlets';
import { registerTestCommand } from '../testing/commands';
import type { StageCommandOutcome } from '../StageCommandOutcome';

const OutletItems = stageComponents['core:outletItems'] as ComponentType<RegisteredComponentProps>;

const rowActions = {
    id: 'row-actions',
    name: 'row-actions',
    componentName: 'core:outletItems',
    properties: {
        name: 'rowActions',
        kind: OutletKind.Actions,
        subject: OutletSubject.Item,
        item: { sku: 'NK-101', status: 'onSale' },
        items: [{ kind: OutletItemKind.Button, id: 'withdraw', title: 'Withdraw', slice: 'Catalog.Products.WithdrawProduct', opens: Opens.None, confirm: 'Withdraw this product?' }],
    },
    slots: {},
} as unknown as ExternalComponent;

// The slices as the Stage reads them from the structure and the command schemas it is given.
const slices = stageActionSlicesOf({
    layouts: [],
    screenTemplates: [],
    screens: [],
    structure: {
        modules: [{
            id: 'Catalog', name: 'Catalog', slices: [],
            features: [{ id: 'Catalog.Products', name: 'Products', features: [], slices: [{ id: 'Catalog.Products.WithdrawProduct', name: 'WithdrawProduct', kind: 'StateChange', command: 'WithdrawProduct' }] }],
        }],
    } as never,
    commandSchemas: { WithdrawProduct: JSON.stringify({ type: 'object', properties: { sku: { type: 'string', 'x-identity': true } }, required: ['sku'] }) },
});

let executeCommand: ReturnType<typeof vi.fn<(name: string, values: Record<string, unknown>) => Promise<StageCommandOutcome>>>;
let refresh: ReturnType<typeof vi.fn<() => void>>;

function draw() {
    render(
        <SliceInvokerProvider invoker={createStageSliceInvoker(executeCommand, true, refresh)}>
            <SceneOutletsProvider value={{ chain: [], slices }}>
                <OutletItems element={rowActions} slots={{}} />
            </SceneOutletsProvider>
        </SliceInvokerProvider>,
    );
}

beforeEach(() => {
    registerTestCommand('WithdrawProduct', '/api/products/withdraw', { sku: String });
    executeCommand = vi.fn(async () => ({ isSuccess: true, validationErrors: [], messages: [], response: undefined }) as unknown as StageCommandOutcome);
    refresh = vi.fn();
});
afterEach(() => cleanup());

describe('when a hook runs its slice directly', () => {
    it('should take the identifier of the command from the item the hook acts on', () =>
        expect(slices).toEqual([{ id: 'Catalog.Products.WithdrawProduct', command: 'WithdrawProduct', inputs: [{ name: 'sku', source: InputSourceKind.DataContext }] }]));

    it('should ask its confirmation before issuing the command', () => {
        draw();
        fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
        expect(screen.getByRole('alertdialog', { name: 'Withdraw' }).textContent).toContain('Withdraw this product?');
        expect(executeCommand).not.toHaveBeenCalled();
    });

    it('should issue the command with the inputs the item resolves once confirmed', async () => {
        draw();
        fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
        fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Withdraw' }));
        await waitFor(() => expect(executeCommand).toHaveBeenCalledWith('WithdrawProduct', { sku: 'NK-101' }));
    });

    it('should refresh the queries on the page after the command succeeds', async () => {
        draw();
        fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
        fireEvent.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Withdraw' }));
        await waitFor(() => expect(refresh).toHaveBeenCalledOnce());
    });

    it('should not issue the command when the confirmation is cancelled', () => {
        draw();
        fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
        fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
        expect(executeCommand).not.toHaveBeenCalled();
    });
});
