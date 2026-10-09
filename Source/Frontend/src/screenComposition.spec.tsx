// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { App } from './App';
import { element } from './testElements';

// The two-item master/detail corpus the screens conformance harness drives: the same element ids, the same
// `data ... by workItemId` declarations, and the routes the Stage registers for it. Each spec below reproduces
// one blocker the harness reported against Stage 4.49.9.

const workItemA = { workItemId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', title: 'Alpha work item', status: 'open' };
const workItemB = { workItemId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', title: 'Bravo work item', status: 'open' };
const commentA = { commentId: '11111111-1111-1111-1111-111111111111', workItemId: workItemA.workItemId, text: 'Comment for alpha' };
const commentB = { commentId: '22222222-2222-2222-2222-222222222222', workItemId: workItemB.workItemId, text: 'Comment for bravo' };

const commandRoutes = {
    CreateWorkItem: '/api/workspaces/tracking/create-work-item',
    RenameWorkItem: '/api/workspaces/tracking/rename-work-item',
    AddComment: '/api/workspaces/tracking/add-comment',
};
const queryRoutes = {
    AllWorkItems: '/api/workspaces/tracking/all-work-items',
    CommentsForWorkItem: '/api/workspaces/tracking/comments-for-work-item',
};

const uuid = { type: 'string', format: 'uuid' };

function listTable(id: string) {
    return element(id, 'core:table', {
        typeName: 'WorkItemSummary',
        route: queryRoutes.AllWorkItems,
        navigateOnRowClickToScreen: 'WorkItemDetails',
        navigateOnRowClickByParameter: 'workItemId',
    }, {
        columns: [element(`${id}.0-column`, 'core:column', { label: 'Title', property: 'title' })],
    });
}

function action(id: string, command: keyof typeof commandRoutes, label: string | null, schema: Record<string, unknown>, fields: { name: string; label?: string }[]) {
    return element(id, 'core:action', {
        command,
        label,
        route: commandRoutes[command],
        method: 'POST',
        schema: JSON.stringify(schema),
        fields,
        metadataStatus: 'available',
    });
}

const createAction = action('WorkItemList.details.0-section.1-action', 'CreateWorkItem', null,
    { type: 'object', properties: { workItemId: uuid, title: { type: 'string' } }, required: ['workItemId', 'title'] },
    [{ name: 'workItemId' }, { name: 'title' }]);
const renameAction = action('WorkItemDetails.details.1-section.2-action', 'RenameWorkItem', 'Rename',
    { type: 'object', properties: { workItemId: uuid, title: { type: 'string' } }, required: ['workItemId', 'title'] },
    [{ name: 'workItemId' }, { name: 'title', label: 'New title' }]);
const addCommentAction = action('WorkItemDetails.details.2-section.2-section.1-section.0-action', 'AddComment', null,
    { type: 'object', properties: { commentId: uuid, workItemId: uuid, text: { type: 'string' } }, required: ['commentId', 'workItemId', 'text'] },
    [{ name: 'commentId' }, { name: 'workItemId' }, { name: 'text' }]);

const scene = {
    layouts: [],
    screenTemplates: [],
    screens: [
        {
            name: 'WorkItemList',
            layout: 'AppShell',
            screenTemplate: null,
            forms: [],
            contributions: [],
            behaviors: [],
            slotContent: {
                content: [
                    element('WorkItemList.list.1-data', 'core:data', { typeName: 'WorkItemSummary', query: 'AllWorkItems', by: null, isCollection: true }),
                    listTable('WorkItemList.list.2-table'),
                    createAction,
                ],
            },
        },
        {
            name: 'WorkItemDetails',
            layout: 'AppShell',
            screenTemplate: null,
            forms: [],
            contributions: [],
            behaviors: [],
            slotContent: {
                content: [
                    element('WorkItemDetails.list.0-data', 'core:data', { typeName: 'WorkItemSummary', query: 'AllWorkItems', by: null, isCollection: true }),
                    listTable('WorkItemDetails.list.1-table'),
                    element('WorkItemDetails.details.1-section', 'core:section', { name: 'summary' }, { content: [renameAction] }),
                    element('WorkItemDetails.details.2-section', 'core:section', { name: 'comments' }, {
                        content: [
                            element('WorkItemDetails.details.2-section.1-data', 'core:data', {
                                typeName: 'CommentView',
                                query: 'CommentsForWorkItem',
                                by: 'workItemId',
                                isCollection: true,
                                route: '/api/workspaces/tracking/all-comment-views',
                            }),
                            element('WorkItemDetails.details.2-section.2-section', 'core:section', { name: 'hierarchy' }, {
                                content: [
                                    element('WorkItemDetails.details.2-section.2-section.0-table', 'core:table', {
                                        typeName: 'CommentView',
                                        route: '/api/workspaces/tracking/all-comment-views',
                                    }, {
                                        columns: [element('WorkItemDetails.details.2-section.2-section.0-table.0-column', 'core:column', { label: 'Comment', property: 'text' })],
                                    }),
                                    element('WorkItemDetails.details.2-section.2-section.1-section', 'core:section', { name: 'composer' }, { content: [addCommentAction] }),
                                ],
                            }),
                        ],
                    }),
                ],
            },
        },
    ],
};

interface Backend {
    workItems: typeof workItemA[];
    comments: typeof commentA[];
    requests: { url: string; method: string; body?: string }[];
    delayed: Map<string, () => void>;
    delay: (workItemId: string) => void;
}

function commandResult() {
    return {
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        response: null,
    };
}

function respond(payload: unknown): Promise<Response> {
    return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(payload) } as Response);
}

function startBackend(): Backend {
    const delays = new Set<string>();
    const backend: Backend = {
        workItems: [{ ...workItemA }, { ...workItemB }],
        comments: [{ ...commentA }, { ...commentB }],
        requests: [],
        delayed: new Map(),
        delay: workItemId => { delays.add(workItemId); },
    };

    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = init?.method ?? 'GET';
        const body = typeof init?.body === 'string' ? init.body : undefined;
        backend.requests.push({ url, method, body });

        if (url === 'stage/scene') return respond(scene);
        if (url === 'stage/routes') return respond({ commands: commandRoutes, queries: queryRoutes });
        if (url === 'api/workspaces/tracking/all-work-items') return respond({ data: backend.workItems.map(item => ({ ...item })) });
        if (url.startsWith('api/workspaces/tracking/comments-for-work-item?')) {
            const workItemId = new URLSearchParams(url.split('?')[1]).get('workItemId') ?? '';
            const answer = () => respond({ data: backend.comments.filter(comment => comment.workItemId === workItemId) });
            if (delays.has(workItemId)) {
                delays.delete(workItemId);
                return new Promise<Response>(resolve => backend.delayed.set(workItemId, () => { void answer().then(resolve); }));
            }

            return answer();
        }

        if (method === 'POST' && url.endsWith(commandRoutes.RenameWorkItem)) {
            const command = JSON.parse(body ?? '{}') as { workItemId: string; title: string };
            backend.workItems = backend.workItems.map(item => item.workItemId === command.workItemId ? { ...item, title: command.title } : item);
            return respond(commandResult());
        }

        if (method === 'POST' && url.endsWith(commandRoutes.AddComment)) {
            const command = JSON.parse(body ?? '{}') as typeof commentA;
            backend.comments = [...backend.comments, command];
            return respond(commandResult());
        }

        if (method === 'POST' && url.endsWith(commandRoutes.CreateWorkItem)) return respond(commandResult());

        return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response);
    }));

    return backend;
}

function renderStage() {
    return render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);
}

function row(text: string) {
    return screen.getByText(text).closest('tr')!;
}

function commentsTable() {
    return screen.getByRole('table', { name: 'CommentView' });
}

function apiRequests(backend: Backend) {
    return backend.requests.filter(request => request.url.includes('api/'));
}

describe('when driving the two-item screen composition corpus', () => {
    beforeEach(() => globalThis.history.replaceState(null, '', '#/WorkItemList'));

    afterEach(() => {
        globalThis.history.replaceState(null, '', '#');
        vi.unstubAllGlobals();
    });

    it('navigates a list row to its details, carrying the selected identity in the address, requests and comments', async () => {
        const backend = startBackend();
        renderStage();

        fireEvent.click(await screen.findByText(workItemB.title));

        expect(await screen.findByText(commentB.text)).toBeDefined();
        expect(screen.queryByText(commentA.text)).toBeNull();
        expect(globalThis.location.hash).toEqual(`#/WorkItemDetails?workItemId=${workItemB.workItemId}`);
        expect(await screen.findByRole('button', { name: 'Clear selection' })).toBeDefined();
        await waitFor(() => expect(row(workItemB.title).getAttribute('aria-selected')).toEqual('true'));
        expect(apiRequests(backend).some(request => request.url.includes(workItemB.workItemId))).toBe(true);
        expect(apiRequests(backend).some(request => request.url.includes('all-comment-views'))).toBe(false);
    });

    it('restores the selected row and its comments from a deep link', async () => {
        globalThis.history.replaceState(null, '', `#/WorkItemDetails?workItemId=${workItemB.workItemId}`);
        startBackend();
        renderStage();

        expect(await screen.findByText(commentB.text)).toBeDefined();
        await waitFor(() => expect(row(workItemB.title).getAttribute('aria-selected')).toEqual('true'));
        expect(screen.queryByText(commentA.text)).toBeNull();
    });

    it('discards a delayed A response after B is selected, and clears comments and the address on clear', async () => {
        globalThis.history.replaceState(null, '', '#/WorkItemDetails');
        const backend = startBackend();
        backend.delay(workItemA.workItemId);
        renderStage();

        fireEvent.click(await screen.findByText(workItemA.title));
        await waitFor(() => expect(backend.delayed.has(workItemA.workItemId)).toBe(true));
        fireEvent.click(screen.getByText(workItemB.title));
        expect(await screen.findByText(commentB.text)).toBeDefined();

        await act(async () => {
            backend.delayed.get(workItemA.workItemId)!();
            await new Promise(resolve => setTimeout(resolve, 0));
        });
        expect(screen.queryByText(commentA.text)).toBeNull();
        expect(screen.getByText(commentB.text)).toBeDefined();

        const before = apiRequests(backend).length;
        fireEvent.click(screen.getByRole('button', { name: 'Clear selection' }));
        await waitFor(() => expect(screen.queryByText(commentB.text)).toBeNull());
        expect(screen.queryByRole('button', { name: 'Clear selection' })).toBeNull();
        expect(globalThis.location.hash).toEqual('#/WorkItemDetails');
        expect(apiRequests(backend).slice(before).some(request => request.url.includes('comments'))).toBe(false);
    });

    it('renames the selected work item through the native form and projects the new title', async () => {
        globalThis.history.replaceState(null, '', `#/WorkItemDetails?workItemId=${workItemB.workItemId}`);
        const backend = startBackend();
        renderStage();
        await screen.findByText(commentB.text);

        fireEvent.click(screen.getByRole('button', { name: 'Rename' }));
        const workItemId = await screen.findByLabelText('workItemId') as HTMLInputElement;
        expect(workItemId.value).toEqual(workItemB.workItemId);
        fireEvent.change(screen.getByLabelText('New title'), { target: { value: 'Renamed bravo' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Rename' }));

        await waitFor(() => expect(backend.requests.filter(request => request.method === 'POST')).toHaveLength(1));
        const posted = JSON.parse(backend.requests.find(request => request.method === 'POST')!.body!) as Record<string, unknown>;
        expect(posted).toEqual({ workItemId: workItemB.workItemId, title: 'Renamed bravo' });
        expect((await screen.findAllByText('Renamed bravo')).length).toBeGreaterThan(0);
    });

    it('adds a comment with its modeled comment identity for the selected work item and projects it', async () => {
        globalThis.history.replaceState(null, '', `#/WorkItemDetails?workItemId=${workItemB.workItemId}`);
        const backend = startBackend();
        renderStage();
        await screen.findByText(commentB.text);

        fireEvent.click(screen.getByRole('button', { name: 'AddComment' }));
        const commentId = await screen.findByLabelText('commentId') as HTMLInputElement;
        expect(commentId.value).toEqual('');
        expect((screen.getByLabelText('workItemId') as HTMLInputElement).value).toEqual(workItemB.workItemId);

        fireEvent.click(screen.getByRole('button', { name: 'Execute AddComment' }));
        await waitFor(() => expect(commentId.getAttribute('aria-invalid')).toEqual('true'));
        expect(backend.requests.filter(request => request.method === 'POST')).toHaveLength(0);

        fireEvent.change(commentId, { target: { value: '33333333-3333-3333-3333-333333333333' } });
        fireEvent.change(screen.getByLabelText('text'), { target: { value: 'Native comment on bravo' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute AddComment' }));

        await waitFor(() => expect(backend.requests.filter(request => request.method === 'POST')).toHaveLength(1));
        const posted = JSON.parse(backend.requests.find(request => request.method === 'POST')!.body!) as Record<string, unknown>;
        expect(posted).toEqual({ commentId: '33333333-3333-3333-3333-333333333333', workItemId: workItemB.workItemId, text: 'Native comment on bravo' });
        expect(await within(commentsTable()).findByText('Native comment on bravo')).toBeDefined();
    });

    it('does not carry a selected identity into a create form on a screen without one', async () => {
        const backend = startBackend();
        renderStage();
        await screen.findByText(workItemB.title);

        fireEvent.click(screen.getByRole('button', { name: 'CreateWorkItem' }));
        expect(((await screen.findByLabelText('workItemId')) as HTMLInputElement).value).toEqual('');
        fireEvent.click(screen.getByRole('button', { name: 'Execute CreateWorkItem' }));
        await waitFor(() => expect(screen.getByLabelText('title').getAttribute('aria-invalid')).toEqual('true'));
        expect(backend.requests.filter(request => request.method === 'POST')).toHaveLength(0);
    });
});
