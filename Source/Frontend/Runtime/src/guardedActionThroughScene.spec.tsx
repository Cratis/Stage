// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { registerTestQueries } from './testing/queries';
import { queryResult } from './testing/queries';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { SceneElement } from '@cratis/scene.model';
import { SceneElementView } from '@cratis/scene.react';
import { canEvaluateGuard, evaluateGuard } from '@cratis/scene.engine';
import { PrimeReactProvider } from '@primereact/core';
import { stageApplicationRegistry } from './App';
import { StageDataProvider, useStageData } from './stageData';
import { bindDataSources } from './stageDataSources';
import { element } from './testElements';
import { canEvaluate, evaluateCondition } from './stageGuards';
import { registerTestCommand, commandResult } from './testing/commands';

const workItemId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const routes = { commands: { CloseWorkItem: '/api/close-work-item' }, queries: { GetWorkItem: '/api/get-work-item' } };

// The Close action exactly as Stage emits it into the Scene: subject references, not bindings.
const emittedClose = {
    label: 'Close',
    alternatives: [{ command: 'CloseWorkItem', condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'open' }, arguments: { workItemId: { subject: 'item.workItemId' } } }],
    otherwise: { outcome: 'Hidden', arguments: {} },
};

// The earlier binding-shaped form, which Scene's renderer resolves away before the component sees it.
const bindingShapedClose = {
    label: 'Close',
    alternatives: [{ command: 'CloseWorkItem', condition: { kind: 'comparison', left: { path: 'item.status' }, operator: 'Equal', right: 'open' }, arguments: { workItemId: { path: 'item.workItemId' } } }],
    otherwise: { outcome: 'Hidden', arguments: {} },
};

function details(properties: Record<string, unknown>): SceneElement {
    return bindDataSources(element('details', 'core:section', {}, {
        content: [
            element('details-data', 'core:data', { typeName: 'WorkItemDetails', query: 'GetWorkItem', by: 'workItemId', isCollection: false }),
            element('close', 'core:action', properties),
        ],
    }));
}

function ThroughScene({ tree }: { tree: SceneElement }) {
    const data = useStageData();
    return <SceneElementView element={tree} registry={stageApplicationRegistry()} resolveBinding={data.resolveBinding} />;
}

function renderThroughScene(tree: SceneElement) {
    registerTestQueries(routes.queries);
    registerTestCommand('CloseWorkItem', routes.commands.CloseWorkItem, { workItemId: { type: 'string' } });
    globalThis.location.hash = `#/WorkItemDetails?workItemId=${workItemId}`;
    return render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} parameters={{ workItemId }} locale='en' locales={['en']} screen='WorkItemDetails'>
                <ThroughScene tree={tree} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
}

function respond(status: string) {
    const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => commandResult() });
        if (String(input).startsWith('/api/get-work-item') || String(input).startsWith('http://localhost:3000/api/get-work-item')) return Promise.resolve({ ok: true, json: async () => (queryResult({ data: { workItemId, status } })) });
        return Promise.resolve({ ok: false, json: async () => ({}) });
    });
    vi.stubGlobal('fetch', fetched);
    return fetched;
}

describe('a guarded action rendered through Scene', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('runs its command for the item when the guard holds', async () => {
        const fetched = respond('open');
        renderThroughScene(details(emittedClose));

        const close = await screen.findByRole('button', { name: 'Close' });
        expect((close as HTMLButtonElement).disabled).toBe(false);
        fireEvent.click(close);

        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [route, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(String(route)).toBe('http://localhost:3000/api/close-work-item');
        expect(init?.body).toBe(JSON.stringify({ workItemId }));
    });

    it('offers nothing when the guard does not hold', async () => {
        const fetched = respond('closed');
        renderThroughScene(details(emittedClose));

        await waitFor(() => expect(fetched.mock.calls.some(([input]) => String(input).includes('get-work-item'))).toBe(true));
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(screen.queryByRole('button', { name: 'Close' })).toBeNull();
    });

    it('cannot evaluate a binding-shaped guard, because Scene resolves it before the action renders', async () => {
        respond('open');
        renderThroughScene(details(bindingShapedClose));

        const close = await screen.findByRole('button', { name: 'Close' });
        expect((close as HTMLButtonElement).disabled).toBe(true);
        expect(close.getAttribute('title')).toContain('STAGE-SCENE-ACTION-001');
    });
});

describe('a guarded action over an enumeration rendered through Scene', () => {
    afterEach(() => vi.unstubAllGlobals());

    const enumerated = {
        ...emittedClose,
        alternatives: [{ ...emittedClose.alternatives[0], condition: { ...emittedClose.alternatives[0].condition, enumeration: ['open', 'closed'] } }],
    };

    function respondWithOrdinal(status: number) {
        const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
            if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => commandResult() });
            if (String(input).includes('get-work-item')) return Promise.resolve({ ok: true, json: async () => (queryResult({ data: { workItemId, status } })) });
            return Promise.resolve({ ok: false, json: async () => ({}) });
        });
        vi.stubGlobal('fetch', fetched);
        return fetched;
    }

    it('offers Close for the ordinal of the member the guard names', async () => {
        respondWithOrdinal(0);
        renderThroughScene(details(enumerated));

        const close = await screen.findByRole('button', { name: 'Close' });
        expect((close as HTMLButtonElement).disabled).toBe(false);
    });

    it('offers nothing for the ordinal of another member', async () => {
        const fetched = respondWithOrdinal(1);
        renderThroughScene(details(enumerated));

        await waitFor(() => expect(fetched.mock.calls.some(([input]) => String(input).includes('get-work-item'))).toBe(true));
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(screen.queryByRole('button', { name: 'Close' })).toBeNull();
    });

    it('does not hold for an ordinal without the enumeration', async () => {
        const fetched = respondWithOrdinal(0);
        renderThroughScene(details(emittedClose));

        await waitFor(() => expect(fetched.mock.calls.some(([input]) => String(input).includes('get-work-item'))).toBe(true));
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(screen.queryByRole('button', { name: 'Close' })).toBeNull();
    });
});

describe('a guarded action over an explicitly numbered enumeration', () => {
    afterEach(() => vi.unstubAllGlobals());

    const numbered = (members: string[], numbers: number[], right: string, operator = 'Equal') => ({
        ...emittedClose,
        alternatives: [{ ...emittedClose.alternatives[0], condition: { ...emittedClose.alternatives[0].condition, operator, right, enumeration: members, enumerationNumbers: numbers } }],
    });

    function respondWithNumber(status: number) {
        const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
            if (init?.method === 'POST') return Promise.resolve({ ok: true, json: async () => commandResult() });
            if (String(input).includes('get-work-item')) return Promise.resolve({ ok: true, json: async () => (queryResult({ data: { workItemId, status } })) });
            return Promise.resolve({ ok: false, json: async () => ({}) });
        });
        vi.stubGlobal('fetch', fetched);
        return fetched;
    }

    async function offered(properties: Record<string, unknown>, status: number): Promise<boolean> {
        const fetched = respondWithNumber(status);
        renderThroughScene(details(properties));
        await waitFor(() => expect(fetched.mock.calls.some(([input]) => String(input).includes('get-work-item'))).toBe(true));
        await new Promise(resolve => setTimeout(resolve, 100));
        const close = screen.queryByRole('button', { name: 'Close' });
        return close !== null && !(close as HTMLButtonElement).disabled;
    }

    it('offers Close for the number reordered onto the named member', async () => {
        expect(await offered(numbered(['open', 'closed'], [1, 0], 'open'), 1)).toBe(true);
    });

    it('offers nothing for the declaration position that is now another member', async () => {
        expect(await offered(numbered(['open', 'closed'], [1, 0], 'open'), 0)).toBe(false);
    });

    it('offers Close for a sparse number', async () => {
        expect(await offered(numbered(['draft', 'issued', 'cancelled'], [-1, 4, 90], 'issued'), 4)).toBe(true);
    });

    it('offers nothing for a sparse member whose declaration position is the wire value', async () => {
        expect(await offered(numbered(['draft', 'issued', 'cancelled'], [-1, 4, 90], 'issued'), 1)).toBe(false);
    });

    it('offers nothing for an unknown wire number', async () => {
        expect(await offered(numbered(['draft', 'issued', 'cancelled'], [-1, 4, 90], 'issued'), 7)).toBe(false);
    });

    it('offers nothing for an unknown wire number even when the guard is NotEqual', async () => {
        expect(await offered(numbered(['draft', 'issued', 'cancelled'], [-1, 4, 90], 'issued', 'NotEqual'), 7)).toBe(false);
    });
});

describe('the Stage guard evaluator against the Scene engine over explicit enumeration numbers', () => {
    const comparison = (members: string[], numbers: number[], operator: string, right: string) => ({
        kind: 'comparison', left: { subject: 'item.status' }, operator, right, enumeration: members, enumerationNumbers: numbers,
    });
    const reordered = ['open', 'closed'];
    const sparse = ['draft', 'issued', 'cancelled'];
    const cases: [string, ReturnType<typeof comparison>, unknown][] = [
        ['reordered open by its number', comparison(reordered, [1, 0], 'Equal', 'open'), 1],
        ['reordered open by its position', comparison(reordered, [1, 0], 'Equal', 'open'), 0],
        ['reordered closed by its number', comparison(reordered, [1, 0], 'NotEqual', 'open'), 0],
        ['sparse negative member', comparison(sparse, [-1, 4, 90], 'Equal', 'draft'), -1],
        ['sparse member by its number', comparison(sparse, [-1, 4, 90], 'Equal', 'issued'), 4],
        ['sparse member by its position', comparison(sparse, [-1, 4, 90], 'Equal', 'issued'), 1],
        ['unknown number with Equal', comparison(sparse, [-1, 4, 90], 'Equal', 'issued'), 7],
        ['unknown number with NotEqual', comparison(sparse, [-1, 4, 90], 'NotEqual', 'issued'), 7],
        ['member name on the wire', comparison(sparse, [-1, 4, 90], 'Equal', 'issued'), 'ISSUED'],
    ];

    it.each(cases)('agrees on %s', (_name, condition, status) => {
        const subject = { item: { status } };
        expect(evaluateCondition(condition, subject)).toBe(evaluateGuard(condition as never, subject));
    });

    it('agrees that malformed numbers cannot be evaluated', () => {
        const duplicated = comparison(sparse, [4, 4, 90], 'Equal', 'issued');
        expect(canEvaluate([duplicated])).toBe(canEvaluateGuard(duplicated as never));
        expect(canEvaluate([duplicated])).toBe(false);
    });
});
