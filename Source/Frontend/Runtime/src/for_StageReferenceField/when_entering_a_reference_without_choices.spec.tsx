// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { PrimeReactProvider } from '@primereact/core';
import { chai, describe, it, vi } from 'vitest';
import { StageDataProvider } from '../stageData';
import { RegisteredForm, commandResult } from '../testing/commands';
import { registerTestQueries } from '../testing/queries';
import { element } from '../testElements';

chai.should();

function show(value: string | number, queryFails: boolean) {
    const reference = { query: 'ManualOrganisations', value: 'organisationId', label: 'name' };
    const schema = JSON.stringify({ type: 'object', required: ['organisationId'], properties: {
        organisationId: { type: typeof value === 'number' ? 'integer' : 'string', 'x-concept': 'OrganisationKey', 'x-reference': reference },
    } });
    const queries: Record<string, string> = queryFails ? { ManualOrganisations: '/queries/manual-organisations' } : {};
    registerTestQueries(queries);
    const fetched = vi.fn(async (_input: unknown, init?: RequestInit) => {
        if (init?.method === 'POST') return { ok: true, json: async () => commandResult() };
        throw new Error('Offline');
    });
    vi.stubGlobal('fetch', fetched);
    render(<PrimeReactProvider><StageDataProvider locale='en' locales={['en']} screen='ManualReferences' routes={{ commands: { RegisterManualReference: '/commands/manual-reference' }, queries }}>
        <RegisteredForm element={element('manual-reference', 'core:command-form', {
            command: 'RegisterManualReference', submitLabel: 'Submit', schema, fields: [{ name: 'organisationId', label: 'Organisation' }],
        })} />
    </StageDataProvider></PrimeReactProvider>);
    return fetched;
}

for (const queryFails of [false, true]) {
    describe(queryFails ? 'when the reference query fails' : 'when the reference query has no registered route', () => {
        it.each([42, 0, '42', '0', 'organisation-one'])('should preserve the scalar type of manual key %s', async value => {
            const fetched = show(value, queryFails);
            if (queryFails) await screen.findByText('Offline');
            const field = screen.getByLabelText('Organisation');
            fireEvent.change(field, { target: { value: String(value) } });
            fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
            await waitFor(() => fetched.mock.calls.some(([, init]) => init?.method === 'POST').should.equal(true));
            const posted = fetched.mock.calls.find(([, init]) => init?.method === 'POST')!;
            JSON.parse(String(posted[1]?.body)).should.deep.equal({ organisationId: value });
        });

        it.each([42, '42'])('should keep a cleared manual key absent rather than turn it into zero (%s)', async value => {
            const fetched = show(value, queryFails);
            if (queryFails) await screen.findByText('Offline');
            const field = screen.getByLabelText('Organisation');
            fireEvent.change(field, { target: { value: String(value) } });
            fireEvent.change(field, { target: { value: '' } });
            await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Submit' })); });
            fetched.mock.calls.some(([, init]) => init?.method === 'POST').should.equal(false);
        });
    });
}
