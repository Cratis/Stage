// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ExternalComponent } from '@cratis/scene.model';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { StageDataProvider, useStageData } from '../stageData';
import { RegisteredForm, commandResult } from '../testing/commands';
import { queryResult, registerTestQueries } from '../testing/queries';

const key = { type: 'integer', 'x-concept': 'OrganisationNumber', 'x-reference': { query: 'NumericOrganisations', value: 'organisationId', label: 'name' } };
const element = { id: 'numeric-reference', componentName: 'core:command-form', properties: {
    command: 'RegisterNumericReference', submitLabel: 'Submit', schema: JSON.stringify({ type: 'object', properties: { organisationId: key }, required: ['organisationId'] }),
    fields: [{ name: 'organisationId', label: 'Organisation' }],
}, slots: {} } as unknown as ExternalComponent;
let data: ReturnType<typeof useStageData>;
function Capture() { data = useStageData(); return null; }
function show() {
    return render(<StageDataProvider locale='en' locales={['en']} screen='NumericReferences' routes={{ commands: { RegisterNumericReference: '/commands/numeric-reference' }, queries: { NumericOrganisations: '/queries/numeric-organisations' } }}>
        <Capture /><RegisteredForm element={element} />
    </StageDataProvider>);
}
async function submit(expected: number) {
    fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => {
        const call = vi.mocked(fetch).mock.calls.filter(([, init]) => init?.method === 'POST').at(-1);
        expect(call).toBeDefined();
        expect(JSON.parse(String(call![1]!.body))).toEqual({ organisationId: expected });
    });
}

describe('numeric reference keys', () => {
    beforeEach(() => {
        sessionStorage.clear();
        registerTestQueries({ NumericOrganisations: '/queries/numeric-organisations' });
        vi.stubGlobal('fetch', vi.fn(async (_input: unknown, init?: RequestInit) => init?.method === 'POST'
            ? { ok: true, json: async () => commandResult() }
            : { ok: true, json: async () => queryResult({ data: [{ organisationId: 42, name: 'Forty two' }, { organisationId: 0, name: 'Zero' }] }) }));
    });

    it.each([42, 0])('should submit query choice %i as a number', async value => {
        show();
        const field = await screen.findByLabelText('Organisation');
        await screen.findByRole('option', { name: 'Zero' });
        fireEvent.change(field, { target: { value: String(value) } });
        await submit(value);
    });

    it('should submit a zero selected after mount and clear it on deselection', async () => {
        show();
        await act(async () => { data.selectRow('numeric', { organisationId: 0 }); });
        await waitFor(() => expect(screen.queryByLabelText('Organisation')).toBeNull());
        await submit(0);
        await act(async () => { data.clearSelection('numeric'); });
        const field = await screen.findByLabelText('Organisation');
        await waitFor(() => expect((field as HTMLSelectElement).value).toBe(''));
        vi.mocked(fetch).mockClear();
        await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Submit' })); });
        expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
    });
});
