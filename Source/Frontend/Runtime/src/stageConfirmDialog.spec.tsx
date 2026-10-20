// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { StageDataProvider } from './stageData';
import { StageConfirmDialog } from './StageConfirmDialog';
import { asDialogScreens } from './stageDialogScreens';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const orderCommand = (extra: Record<string, unknown> = {}) => JSON.stringify({
    type: 'object',
    required: ['orderId'],
    properties: { orderId: { type: 'string', format: 'uuid', 'x-concept': 'OrderId' }, ...extra },
});
const routes = {
    commands: { ApproveOrder: '/api/approve', RejectOrder: '/api/reject' },
    queries: {},
    commandSchemas: {
        ApproveOrder: orderCommand(),
        RejectOrder: orderCommand({ reason: { type: 'string' } }),
    },
};
const action = (command: string) => element(`action-${command}`, 'core:action', { command, navigateToScreen: 'ApprovalInbox' });
const decision = {
    name: 'Decision', layout: 'AppShell', screenTemplate: 'ConfirmDialog', contributions: [],
    slotContent: { actions: [action('ApproveOrder'), action('RejectOrder')] },
    forms: [
        { name: 'ApproveOrderForm', forCommand: 'ApproveOrder', fields: [{ name: 'Order', sourceProperty: 'orderId' }] },
        { name: 'RejectOrderForm', forCommand: 'RejectOrder', fields: [{ name: 'Order', sourceProperty: 'orderId' }, { name: 'Reason', sourceProperty: 'reason' }] },
    ],
} as unknown as Screen;

function show() {
    registerStageCommands(routes);
    const [dialog] = asDialogScreens([decision], []);
    const content = (dialog.slotContent.content as ExternalComponent[])[0];
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={{ orderId: 'o1' }} locale='en' locales={['en']} screen='Decision'>
                <StageConfirmDialog element={content} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}
const field = (name: string) => document.querySelector(`.stage-command-form__field[data-field="${name}"]`);

describe('the decision dialog of a ConfirmDialog screen', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); window.location.hash = ''; });

    it('shows a titled dialog with one humanized button for each decision and no raw form', () => {
        show();

        const dialog = screen.getByRole('dialog');
        expect(within(dialog).getByRole('heading', { name: 'Decision' })).toBeDefined();
        expect(within(dialog).getAllByRole('button').map(_ => _.textContent)).toEqual(['Approve order', 'Reject order']);
        expect(screen.queryByText(/Execute/)).toBeNull();
        expect(screen.queryByText('ApproveOrder')).toBeNull();
        expect(field('Order')).toBeNull();
        expect(document.querySelector('form')).toBeNull();
    });

    it('shows only the form of the chosen decision, without asking for the order', () => {
        show();

        fireEvent.click(screen.getByRole('button', { name: 'Reject order' }));

        expect(document.querySelectorAll('form')).toHaveLength(1);
        expect(field('Reason')).not.toBeNull();
        expect(field('Order')).toBeNull();
        expect(screen.queryByText(/Execute/)).toBeNull();
    });

    it('submits the command with the order and then goes to the screen the action names', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        show();

        fireEvent.click(screen.getByRole('button', { name: 'Reject order' }));
        fireEvent.change(screen.getByLabelText('Reason'), { target: { value: 'too large' } });
        fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));

        await waitFor(() => expect(fetched).toHaveBeenCalled());
        const call = fetched.mock.calls.find(([url]) => String(url).includes('/api/reject'))!;
        expect(JSON.parse(String((call[1] as RequestInit).body))).toEqual(expect.objectContaining({ orderId: 'o1', reason: 'too large' }));
        await waitFor(() => expect(window.location.hash).toContain('ApprovalInbox'));
    });
});
