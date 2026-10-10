// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ActionDispatcher } from '@cratis/scene.engine';
import { InteractionTriggerKind, type Behavior, type SceneElement } from '@cratis/scene.model';
import { InteractionScope } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { StageAction, StageTable } from './stageComponents';
import { StageDataProvider } from './stageData';
import { bindDataSources } from './stageDataSources';
import { guardPath } from './stageGuards';
import { doubleClickWindow } from './stageRowInteractions';
import { element } from './testElements';

const workItemId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const routes = {
    commands: { CloseWorkItem: '/api/close-work-item', ReopenWorkItem: '/api/reopen-work-item' },
    queries: { GetWorkItem: '/api/get-work-item', AllWorkItems: '/api/all-work-items' },
};
const isOpen = { kind: 'comparison', left: { path: 'item.status' }, operator: 'Equal', right: 'open' };

function closeAction(properties: Record<string, unknown> = {}) {
    return element('close', 'core:action', {
        label: 'Close',
        navigateToScreen: 'WorkItemList',
        alternatives: [{ command: 'CloseWorkItem', condition: isOpen, arguments: { workItemId: { path: 'item.workItemId' } } }],
        otherwise: { outcome: 'Hidden', arguments: {} },
        ...properties,
    });
}

// The screen as the Stage serves it: a single-item data declaration read by the screen parameter, then the action.
function details(action: SceneElement): SceneElement {
    return bindDataSources(element('details', 'core:section', {}, {
        content: [
            element('details-data', 'core:data', { typeName: 'WorkItemDetails', query: 'GetWorkItem', by: 'workItemId', isCollection: false }),
            element('summary', 'core:section', {}, { content: [action] }),
        ],
    }));
}

function withData(ui: ReactElement) {
    globalThis.location.hash = `#/WorkItemDetails?workItemId=${workItemId}`;
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={{ workItemId }} locale='en' locales={['en']} screen='WorkItemDetails'>{ui}</StageDataProvider>
        </PrimeReactProvider>,
    );
}

function respond(status: string) {
    const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => ({ isSuccess: true, validationResults: [] }) });
        if (String(input).startsWith('api/get-work-item')) return Promise.resolve({ ok: true, json: async () => ({ data: { workItemId, status } }) });
        return Promise.resolve({ ok: false, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetched);
    return fetched;
}

function actionIn(tree: SceneElement): SceneElement {
    const found = (node: SceneElement): SceneElement | undefined => {
        const component = node as { componentName?: string; slots?: Record<string, SceneElement[]> };
        if (component.componentName === 'core:action') return node;
        for (const children of Object.values(component.slots ?? {})) for (const child of children) {
            const match = found(child);
            if (match) return match;
        }
        return undefined;
    };
    return found(tree)!;
}

describe('a guarded action', () => {
    beforeEach(() => vi.useRealTimers());

    it('is bound to the item its nearest data declaration reads', () => {
        const bound = actionIn(details(closeAction())) as { properties: Record<string, unknown> };
        expect(bound.properties.itemQuery).toBe('GetWorkItem');
        expect(bound.properties.itemArguments).toEqual({ workItemId: { path: 'parameters.workItemId' } });
    });

    it('executes its command with the item values when the guard holds', async () => {
        const fetched = respond('open');
        withData(<StageAction element={actionIn(details(closeAction())) as never} slots={{}} />);

        fireEvent.click(await screen.findByRole('button', { name: 'Close' }));

        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [route, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(String(route)).toBe('/api/close-work-item');
        expect(init?.body).toBe(JSON.stringify({ workItemId }));
        await waitFor(() => expect(globalThis.location.hash).toBe('#/WorkItemList'));
    });

    it('renders nothing when the guard does not hold and the fallback is hidden', async () => {
        const fetched = respond('closed');
        withData(<StageAction element={actionIn(details(closeAction())) as never} slots={{}} />);

        await waitFor(() => expect(fetched).toHaveBeenCalled());
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(screen.queryByRole('button', { name: 'Close' })).toBeNull();
    });

    it('offers the fallback command when the guard does not hold and the fallback executes', async () => {
        const fetched = respond('closed');
        const action = closeAction({ otherwise: { outcome: 'Execute', command: 'ReopenWorkItem', arguments: { workItemId: { path: 'item.workItemId' } } } });
        withData(<StageAction element={actionIn(details(action)) as never} slots={{}} />);

        fireEvent.click(await screen.findByRole('button', { name: 'Close' }));

        await waitFor(() => expect(fetched.mock.calls.some(([route, init]) => init?.method === 'POST' && String(route) === '/api/reopen-work-item')).toBe(true));
    });

    it('stays disabled with the diagnostic when its guard cannot be evaluated', async () => {
        const fetched = respond('open');
        const action = closeAction({ alternatives: [{ command: 'CloseWorkItem', condition: { kind: 'PathComparisonSyntax' }, arguments: {} }] });
        withData(<StageAction element={actionIn(details(action)) as never} slots={{}} />);

        const button = await screen.findByRole('button', { name: 'Close' });
        expect(button.hasAttribute('disabled')).toBe(true);
        expect(screen.getByRole('alert').textContent).toContain('STAGE-SCENE-ACTION-001');
        fireEvent.click(button);
        expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
    });

    it('stays disabled with the diagnostic when it has no data subject', () => {
        respond('open');
        withData(<StageAction element={closeAction()} slots={{}} />);

        expect(screen.getByRole('button', { name: 'Close' }).hasAttribute('disabled')).toBe(true);
        expect(screen.getByRole('alert').textContent).toContain('no data subject');
    });
});

describe('a table with a guarded double click', () => {
    const navigate = { kind: 'Navigate', screen: 'WorkItemDetails', arguments: [{ name: 'workItemId', value: { path: 'item.workItemId' } }] };
    const notify = { kind: 'Notify', level: 'info', message: { text: 'This work item is closed' } };
    const behaviors: Behavior[] = [{
        bindings: [
            { trigger: { kind: InteractionTriggerKind.DoubleClick }, actions: [navigate], condition: { path: guardPath, value: { kind: 'firstMatch', index: 0, alternatives: [isOpen] } } },
            { trigger: { kind: InteractionTriggerKind.DoubleClick }, actions: [notify], condition: { path: guardPath, value: { kind: 'otherwise', alternatives: [isOpen] } } },
        ],
    } as unknown as Behavior];
    const table = { ...element('list', 'core:table', { route: '/api/all-work-items', typeName: 'WorkItemSummary', navigateOnRowClickToScreen: 'WorkItemDetails', navigateOnRowClickByParameter: 'workItemId' }, {
        columns: [element('title', 'core:column', { property: 'title', label: 'Title' })],
    }), behaviors };

    function renderTable(dispatcher: Partial<ActionDispatcher>) {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
            ok: true,
            json: async () => ({ data: [{ workItemId: 'a', title: 'Open item', status: 'open' }, { workItemId: 'b', title: 'Closed item', status: 'closed' }] }),
        }));
        globalThis.location.hash = '#/WorkItemList';
        return render(
            <PrimeReactProvider>
                <StageDataProvider routes={routes} locale='en' locales={['en']} screen='WorkItemList'>
                    <InteractionScope dispatcher={dispatcher as ActionDispatcher} context={{ resolve: () => undefined }} attachments={[]}>
                        <StageTable element={table} slots={{}} />
                    </InteractionScope>
                </StageDataProvider>
            </PrimeReactProvider>,
        );
    }

    it('navigates with the row values when the guard holds for the row', async () => {
        const dispatcher = { navigate: vi.fn(), notify: vi.fn() };
        renderTable(dispatcher);

        fireEvent.doubleClick(await screen.findByText('Open item'));

        await waitFor(() => expect(dispatcher.navigate).toHaveBeenCalledWith('WorkItemDetails', { workItemId: 'a' }));
        expect(dispatcher.notify).not.toHaveBeenCalled();
    });

    it('shows the authored otherwise feedback when the guard does not hold for the row', async () => {
        const dispatcher = { navigate: vi.fn(), notify: vi.fn() };
        renderTable(dispatcher);

        fireEvent.doubleClick(await screen.findByText('Closed item'));

        await waitFor(() => expect(dispatcher.notify).toHaveBeenCalledWith('info', 'This work item is closed'));
        expect(dispatcher.navigate).not.toHaveBeenCalled();
    });

    it('lets a double click cancel the row click it starts with', async () => {
        const dispatcher = { navigate: vi.fn(), notify: vi.fn() };
        renderTable(dispatcher);
        const row = await screen.findByText('Closed item');

        fireEvent.click(row);
        fireEvent.doubleClick(row);
        await new Promise(resolve => setTimeout(resolve, doubleClickWindow + 50));

        expect(globalThis.location.hash).toBe('#/WorkItemList');
    });

    it('still runs a single row click once the double-click window has passed', async () => {
        renderTable({ navigate: vi.fn(), notify: vi.fn() });

        fireEvent.click(await screen.findByText('Open item'));

        await waitFor(() => expect(globalThis.location.hash).toBe('#/WorkItemDetails?workItemId=a'), { timeout: doubleClickWindow + 500 });
    });
});
