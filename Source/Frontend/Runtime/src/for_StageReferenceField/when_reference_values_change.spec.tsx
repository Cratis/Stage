// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ExternalComponent } from '@cratis/scene.model';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { StageDataProvider, useStageData } from '../stageData';
import { RegisteredForm, commandResult } from '../testing/commands';
import { queryResult, registerTestQueries } from '../testing/queries';

const properties = {
    organisationId: { type: 'string', 'x-concept': 'OrganisationId', 'x-reference': { query: 'AllOrganisations', value: 'organisationId', label: 'name' } },
    name: { type: 'string' },
};
const element = {
    id: 'reference-lifecycle', componentName: 'core:command-form', properties: {
        command: 'RegisterReferenceLifecycle', submitLabel: 'Submit', schema: JSON.stringify({ type: 'object', properties, required: ['organisationId', 'name'] }),
        fields: [{ name: 'organisationId', label: 'Organisation' }, { name: 'name', label: 'Name' }],
    }, slots: {},
} as unknown as ExternalComponent;
const rows = [{ organisationId: 'first', name: 'First' }, { organisationId: 'second', name: 'Second' }];
let data: ReturnType<typeof useStageData>;
function Capture() { data = useStageData(); return null; }
function SeededForm() {
    const current = useStageData();
    const [ready, setReady] = useState(false);
    useEffect(() => { current.selectRow('references', rows[0]); setReady(true); }, []);
    return ready ? <RegisteredForm element={element} /> : null;
}
function show(seeded = false) {
    return render(<StageDataProvider locale='en' locales={['en']} screen='References' routes={{ commands: { RegisterReferenceLifecycle: '/commands/reference-lifecycle' }, queries: { AllOrganisations: '/queries/organisations' } }}>
        <Capture />{seeded ? <SeededForm /> : <RegisteredForm element={element} />}
    </StageDataProvider>);
}
async function select(row: Record<string, unknown> | undefined) {
    await act(async () => { data.selectRow('references', row); });
}
async function submit(expected: string) {
    fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => {
        const call = vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST').at(-1);
        expect(call).toBeDefined();
        expect(JSON.parse(String(call![1]!.body))).toMatchObject({ organisationId: expected, name: 'Keep me' });
    });
}

describe('reference values owned by selection', () => {
    beforeEach(() => {
        sessionStorage.clear();
        registerTestQueries({ AllOrganisations: '/queries/organisations' });
        vi.stubGlobal('fetch', vi.fn(async (_input: unknown, init?: RequestInit) => init?.method === 'POST'
            ? { ok: true, json: async () => commandResult() }
            : { ok: true, json: async () => queryResult({ data: rows }) }));
    });

    it('should replace the initial reference after mount and preserve another edit in the payload', async () => {
        show();
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Keep me' } });
        await select(rows[0]);
        await select(rows[1]);
        await submit('second');
    });

    it('should replace a reference already supplied when the form mounted without erasing another edit', async () => {
        show(true);
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Keep me' } });
        await select(rows[1]);
        await submit('second');
    });

    it('should clear a reference supplied by a selection when it is deselected', async () => {
        show();
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Keep me' } });
        await select(rows[0]);
        await select(undefined);
        const field = await screen.findByLabelText('Organisation');
        await waitFor(() => expect((field as HTMLSelectElement).value).toBe(''));
        await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Submit' })); });
        expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
        expect((screen.getByLabelText('Name') as HTMLInputElement).value).toBe('Keep me');
    });

    it('should not erase a person chosen reference when an unrelated selection changes', async () => {
        show();
        const field = await screen.findByLabelText('Organisation');
        await screen.findByRole('option', { name: 'First' });
        fireEvent.change(field, { target: { value: 'first' } });
        fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Keep me' } });
        await select({ unrelated: 'value' });
        await select(undefined);
        await submit('first');
    });
});
