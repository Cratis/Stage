// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ExternalComponent } from '@cratis/scene.model';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { StageDataProvider, useStageData } from './stageData';
import { commandResult } from './testing/commands';
import { StageCommandForm } from './StageCommandForm';
import { registerStageCommands } from '../../src/host/commandRegistration';

const property = { type: 'string', format: 'uuid', 'x-concept': 'ResourceId', 'x-identity': true };
const properties = { resourceId: property, name: { type: 'string' } };
let data: ReturnType<typeof useStageData>;
function Rows() {
    data = useStageData();
    return null;
}
function show(creates: boolean, inline: boolean, askIdentifier = creates) {
    const schema = JSON.stringify({ type: 'object', properties, required: ['resourceId', 'name'], ...(creates ? { 'x-creates': true } : {}) });
    const element = { id: 'intent', componentName: 'core:command-form', properties: {
        command: 'ResourceIntent', submitLabel: 'Submit', rowAction: true, ...(inline ? { schema } : {}),
        fields: [...(askIdentifier ? [{ name: 'resourceId', label: 'Resource' }] : []), { name: 'name', label: 'Name' }],
    }, slots: {} } as unknown as ExternalComponent;
    registerStageCommands({ commands: { ResourceIntent: '/commands/resource-intent' }, queries: {}, commandSchemas: { ResourceIntent: schema } });
    render(<StageDataProvider locale='en' locales={['en']} screen='Resources' routes={{ commands: { ResourceIntent: '/commands/resource-intent' }, queries: {}, commandSchemas: { ResourceIntent: schema } }}>
        <Rows /><StageCommandForm element={element} />
    </StageDataProvider>);
}

describe('creation intent in inline and route schemas', () => {
    beforeEach(() => {
        sessionStorage.clear();
        vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => commandResult() })));
    });

    it.each([true, false])('should keep a caller supplied identity on an empty collection (inline %s)', async inline => {
        show(true, inline);
        await act(async () => { data.registerQueryResult('resources', { name: 'Resources', route: '/queries/resources', rows: [], loading: false, error: '', arguments: {} }); });
        expect((screen.getByLabelText('Resource') as HTMLInputElement).disabled).toBe(false);
        fireEvent.change(screen.getByLabelText('Resource'), { target: { value: 'new-resource' } });
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'New' } });
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
        await waitFor(() => {
            const call = vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST').at(-1);
            expect(call).toBeDefined();
            expect(JSON.parse(String(call![1]!.body))).toEqual({ resourceId: 'new-resource', name: 'New' });
        });
    });

    it.each([true, false])('should ignore selected rows when a schema proves creation (inline %s)', async inline => {
        show(true, inline, false);
        const row = { resourceId: 'existing' };
        await act(async () => {
            data.registerQueryResult('resources', { name: 'Resources', route: '/queries/resources', rows: [row], loading: false, error: '', arguments: {} });
            data.selectRow('resources', row);
        });
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'New' } });
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
        await waitFor(() => {
            const call = vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST').at(-1);
            expect(call).toBeDefined();
            const posted = JSON.parse(String(call![1]!.body));
            expect(typeof posted.resourceId).toBe('string');
            expect(posted.resourceId).not.toBe('existing');
        });
    });

    it.each([true, false])('should keep a true row action bound to the selected row (inline %s)', async inline => {
        show(false, inline);
        const row = { resourceId: 'existing' };
        await act(async () => { data.registerQueryResult('resources', { name: 'Resources', route: '/queries/resources', rows: [row], loading: false, error: '', arguments: {} }); });
        await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Submit' })); });
        expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
        await act(async () => { data.selectRow('resources', row); });
        expect(screen.queryByLabelText('Resource')).toBeNull();
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Changed' } });
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
        await waitFor(() => {
            const call = vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST').at(-1);
            expect(call).toBeDefined();
            expect(JSON.parse(String(call![1]!.body))).toEqual({ resourceId: 'existing', name: 'Changed' });
        });
    });
});
