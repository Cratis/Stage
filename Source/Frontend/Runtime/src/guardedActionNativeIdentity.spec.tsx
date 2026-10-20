// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { Guid } from '@cratis/fundamentals';
import { ObservableQuerySubscription } from '@cratis/arc/queries';
import { registerQueryIdentity } from '@cratis/scene.components';
import { SceneElementView } from '@cratis/scene.react';
import { PrimeReactProvider } from '@primereact/core';
import { AllWorkItems } from './for_stageNavigation/given/AllWorkItems';
import { stageApplicationRegistry } from './App';
import { StageDataProvider, useStageData } from './stageData';
import { bindDataSources } from './stageDataSources';
import { element } from './testElements';
import { commandResult, registerTestCommand } from './testing/commands';
import { queryResult, registerTestQueries } from './testing/queries';

const workItemId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const routes = { commands: { CloseWorkItem: '/api/close' }, queries: { GetWorkItem: '/api/get-work-item' } };
const tree = bindDataSources(element('screen', 'core:section', {}, {
    content: [
        element('items', 'core:table', { query: 'AllWorkItems' }, {
            columns: [element('title', 'core:column', { property: 'title', label: 'Title' })],
        }),
        element('details', 'core:data', { typeName: 'WorkItemDetails', query: 'GetWorkItem', by: 'workItemId', isCollection: false }),
        element('close', 'core:action', {
            label: 'Close',
            alternatives: [{
                command: 'CloseWorkItem',
                condition: { kind: 'comparison', left: { subject: 'item.status' }, operator: 'Equal', right: 'open' },
                arguments: { workItemId: { subject: 'item.workItemId' } },
            }],
            otherwise: { outcome: 'Hidden', arguments: {} },
        }),
    ],
}));

function ThroughScene() {
    const data = useStageData();
    return <SceneElementView element={tree} registry={stageApplicationRegistry()} resolveBinding={data.resolveBinding} />;
}

describe('a guarded action whose screen key comes from a native proxy row', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
        vi.restoreAllMocks();
    });

    it('should query and execute for the resolved Guid without treating it as another binding', async () => {
        registerQueryIdentity('AllWorkItems', 'NativeIdentity.AllWorkItems', AllWorkItems);
        registerTestQueries(routes.queries);
        registerTestCommand('CloseWorkItem', routes.commands.CloseWorkItem, { workItemId: { type: 'string' } });
        const perform = vi.spyOn(AllWorkItems.prototype, 'perform');
        vi.spyOn(AllWorkItems.prototype, 'subscribe').mockImplementation(function (this: AllWorkItems, callback) {
            void this.perform().then(callback);
            return new ObservableQuerySubscription({ connect: vi.fn(), disconnect: vi.fn(), lastPingLatency: 0, averageLatency: 0 });
        });
        const fetched = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
            const url = String(input);
            const payload = init?.method === 'POST' ? commandResult() : queryResult({
                data: url.includes('all-work-items') ? [{ workItemId, title: 'Native item', status: 0 }] : { workItemId, status: 'open' },
            });
            return Promise.resolve({ ok: true, json: async () => payload } as Response);
        });
        vi.stubGlobal('fetch', fetched);
        render(<PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='WorkItemDetails'>
                <ThroughScene />
            </StageDataProvider>
        </PrimeReactProvider>);

        const row = (await screen.findByText('Native item')).closest('tr')!;
        const result = await perform.mock.results[0].value;
        expect(result.data[0].workItemId).toBeInstanceOf(Guid);
        expect(screen.queryByRole('button', { name: 'Close' })).toBeNull();
        fireEvent.click(row);
        fireEvent.click(await screen.findByRole('button', { name: 'Close' }));

        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        expect(fetched.mock.calls.some(([input]) => String(input).includes(`get-work-item?workItemId=${workItemId}`))).toBe(true);
        const [, request] = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
        expect(request?.body).toBe(JSON.stringify({ workItemId }));
        expect(screen.getByText('Native item')).toBeDefined();
    });
});
