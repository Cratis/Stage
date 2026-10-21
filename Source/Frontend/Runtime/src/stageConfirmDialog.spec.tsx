// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import type { ExternalComponent, Screen } from '@cratis/scene.model';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { StageDataProvider, useStageData } from './stageData';
import { StageConfirmDialog } from './StageConfirmDialog';
import { asDialogScreens } from './stageDialogScreens';
import { forgetSessionNavigation } from './stageNavigation';
import { element } from './testElements';
import { commandResult } from './testing/commands';
import { queryResult, registerTestQueries } from './testing/queries';

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

const inbox = { name: 'ApprovalInbox', layout: 'AppShell', contributions: [], slotContent: {}, forms: [] } as unknown as Screen;
const diverging = {
    ...decision,
    slotContent: { actions: [action('ApproveOrder'), element('action-RejectOrder', 'core:action', { command: 'RejectOrder', navigateToScreen: 'Orders' })] },
} as unknown as Screen;
const settled = () => new Promise(resolve => setTimeout(resolve));

function Selecting({ row }: { row: Record<string, unknown> }) {
    const { selectRow } = useStageData();
    useEffect(() => selectRow('orders', row), [row, selectRow]);
    return null;
}

function show(options: { screen?: Screen; routes?: typeof routes; parameters?: Record<string, string>; selected?: Record<string, unknown> } = {}) {
    const given = options.routes ?? routes;
    registerStageCommands(given);
    const [dialog] = asDialogScreens([options.screen ?? decision, inbox], []);
    const content = (dialog.slotContent.content as ExternalComponent[])[0];
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={given} parameters={options.parameters ?? { orderId: 'o1' }} locale='en' locales={['en']} screen='Decision'>
                {options.selected && <Selecting row={options.selected} />}
                <StageConfirmDialog element={content} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}
const bodyOf = (fetched: ReturnType<typeof vi.fn>, route: string): Record<string, unknown> | undefined => {
    const call = fetched.mock.calls.find(([url]) => String(url).includes(route));
    return call ? JSON.parse(String((call[1] as RequestInit).body)) as Record<string, unknown> : undefined;
};
const reference = { type: 'string', format: 'uuid', 'x-concept': 'CustomerId' };
const withReference = {
    ...routes,
    commandSchemas: { ...routes.commandSchemas, ApproveOrder: JSON.stringify({ ...JSON.parse(orderCommand()), required: ['orderId', 'customerId'], properties: { ...JSON.parse(orderCommand()).properties, customerId: reference } }) },
};
const field = (name: string) => document.querySelector(`.stage-command-form__field[data-field="${name}"]`);

describe('the decision dialog of a ConfirmDialog screen', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); window.location.hash = ''; });

    it('shows a titled dialog with one humanized button for each decision and no raw form', () => {
        show();

        const dialog = screen.getByRole('dialog');
        expect(within(dialog).getByRole('heading', { name: 'Decision' })).toBeDefined();
        expect(within(dialog).getAllByRole('button').map(_ => _.textContent)).toEqual(['Approve order', 'Reject order', 'Cancel']);
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

    it('styles the decisions as the theme\'s dialog buttons with the first one emphasized', () => {
        show();

        const approve = screen.getByRole('button', { name: 'Approve order' });
        const reject = screen.getByRole('button', { name: 'Reject order' });
        expect(approve.classList.contains('scene-slice-dialog-submit')).toBe(true);
        expect(approve.dataset.emphasis).toBe('primary');
        expect(reject.classList.contains('scene-slice-dialog-cancel')).toBe(true);
        expect(reject.dataset.emphasis).toBe('secondary');
    });

    it('leaves for the screen the decisions lead to on Cancel, without executing a command', () => {
        const fetched = vi.fn();
        vi.stubGlobal('fetch', fetched);
        show();

        fireEvent.click(screen.getByRole('button', { name: 'Reject order' }));
        fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(window.location.hash).toContain('ApprovalInbox');
        expect(fetched).not.toHaveBeenCalled();
    });

    it('goes back on Cancel when the decisions lead to different screens and the Stage navigated here', async () => {
        await settled();
        forgetSessionNavigation();
        window.dispatchEvent(new HashChangeEvent('hashchange'));
        const back = vi.spyOn(window.history, 'back').mockImplementation(() => undefined);
        show({ screen: diverging });

        fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(back).toHaveBeenCalledOnce();
        back.mockRestore();
    });

    it('goes to the first other screen on Cancel when the dialog is where the Stage was opened, so it never leaves the Stage', async () => {
        await settled();
        forgetSessionNavigation();
        const back = vi.spyOn(window.history, 'back').mockImplementation(() => undefined);
        show({ screen: diverging });

        fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(back).not.toHaveBeenCalled();
        expect(window.location.hash).toBe('#/ApprovalInbox');
        back.mockRestore();
    });

    it('focuses the first decision when it opens', () => {
        show();

        expect(document.activeElement).toBe(screen.getByRole('button', { name: 'Approve order' }));
    });

    it('does not go anywhere when the command is rejected', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult({ isSuccess: false, isValid: false, validationResults: [{ message: 'No', members: [] }] }) });
        vi.stubGlobal('fetch', fetched);
        show();

        fireEvent.click(screen.getByRole('button', { name: 'Approve order' }));
        fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));

        await waitFor(() => expect(fetched).toHaveBeenCalled());
        // Wait until the rejection has been handled - its message shown - before asserting that nothing navigated.
        expect((await screen.findAllByText('No')).length).toBeGreaterThan(0);
        expect(window.location.hash).not.toContain('ApprovalInbox');
    });

    it('asks for a required reference the context does not supply, and invents no identity for it', async () => {
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        show({ routes: withReference });

        fireEvent.click(screen.getByRole('button', { name: 'Approve order' }));
        expect(field('Order')).toBeNull();
        expect(field('customerId')).not.toBeNull();
        fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));

        expect((await screen.findAllByText(/customerId is required/)).length).toBeGreaterThan(0);
        expect(fetched).not.toHaveBeenCalled();
        expect(globalThis.localStorage.getItem('stage.session.CustomerId')).toBeNull();
    });

    it('submits the order of the address, not a stale one the session holds', async () => {
        globalThis.localStorage.setItem('stage.session.OrderId', 'stale');
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        show();

        fireEvent.click(screen.getByRole('button', { name: 'Approve order' }));
        fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));

        await waitFor(() => expect(bodyOf(fetched, '/api/approve')).toBeDefined());
        expect(bodyOf(fetched, '/api/approve')?.orderId).toBe('o1');
    });

    it('submits the order of the selected row, not a stale one the session holds', async () => {
        globalThis.localStorage.setItem('stage.session.OrderId', 'stale');
        const fetched = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult() });
        vi.stubGlobal('fetch', fetched);
        show({ parameters: {}, selected: { orderId: 'row-1' } });

        fireEvent.click(screen.getByRole('button', { name: 'Approve order' }));
        expect(field('Order')).toBeNull();
        fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));

        await waitFor(() => expect(bodyOf(fetched, '/api/approve')).toBeDefined());
        expect(bodyOf(fetched, '/api/approve')?.orderId).toBe('row-1');
    });

    it('still renders the inputs of a form that lists no fields and keeps the generation the form declares', () => {
        const auto = { ...decision, forms: [{ name: 'RejectOrderForm', forCommand: 'RejectOrder', generationMode: 'auto', fields: [] }] } as unknown as Screen;
        show({ screen: auto });

        fireEvent.click(screen.getByRole('button', { name: 'Reject order' }));

        expect(field('reason')).not.toBeNull();
        expect(field('orderId')).toBeNull();
    });

    it('keeps what the form populates from, so it starts from the query answer', async () => {
        const fetched = vi.fn((url: RequestInfo | URL) => Promise.resolve(String(url).includes('draft')
            ? { ok: true, json: async () => queryResult({ data: [{ reason: 'too large' }] }) }
            : { ok: true, json: async () => commandResult() }));
        vi.stubGlobal('fetch', fetched);
        const given = { ...routes, queries: { GetRejection: '/api/draft' } };
        registerTestQueries(given.queries);
        const populated = {
            ...decision,
            forms: [{ name: 'RejectOrderForm', forCommand: 'RejectOrder', fields: [{ name: 'Reason', sourceProperty: 'reason' }], populateSource: { queryName: 'GetRejection', parameterBindings: { orderId: { path: 'orderId' } } } }],
        } as unknown as Screen;
        show({ screen: populated, routes: given });

        fireEvent.click(screen.getByRole('button', { name: 'Reject order' }));

        await waitFor(() => expect((screen.getByLabelText('Reason') as HTMLInputElement).value).toBe('too large'));
    });
});
