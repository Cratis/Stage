// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider } from './stageData';
import { element } from './testElements';
import { RegisteredTable, registerTestQueries, queryResult } from './testing/queries';
import { commandResult } from './testing/commands';

const StageAction = stageComponents['core:action'];
const property = (concept: string, identity = false) => ({ type: 'string', format: 'uuid', 'x-concept': concept, ...(identity ? { 'x-identity': true } : {}) });
// The rendered scene carries the schema on the action without the identity mark; the host's own schema for the command carries it.
const sceneSchema = JSON.stringify({ type: 'object', required: ['opportunityId', 'companyId'], properties: { opportunityId: property('OpportunityId'), companyId: property('CompanyId') } });
const hostSchema = JSON.stringify({ type: 'object', required: ['opportunityId', 'companyId'], properties: { opportunityId: property('OpportunityId', true), companyId: property('CompanyId') } });
const rows = [
    { opportunityId: '7d1c8f3a-0000-4000-8000-000000000001', companyId: '7d1c8f3a-0000-4000-8000-0000000000c1', name: 'Fjord deal' },
    { opportunityId: '7d1c8f3a-0000-4000-8000-000000000002', companyId: '7d1c8f3a-0000-4000-8000-0000000000c2', name: 'Norsk deal' },
];

function renderOpportunities() {
    const routes = { commands: { LinkOpportunityToCompany: '/api/link' }, queries: { ListOpportunities: '/api/opportunities' }, commandSchemas: { LinkOpportunityToCompany: hostSchema } };
    registerStageCommands(routes);
    registerTestQueries(routes.queries);
    const fetched = vi.fn((_: RequestInfo | URL, init?: RequestInit) => init?.method === 'POST'
        ? Promise.resolve({ ok: true, json: async () => commandResult() })
        : Promise.resolve({ ok: true, json: async () => queryResult({ data: rows }) }));
    vi.stubGlobal('fetch', fetched);
    globalThis.location.hash = '#/Opportunities';
    const table = element('opportunities', 'core:table', { query: 'ListOpportunities', typeName: 'OpportunityRow' }, { columns: [element('name', 'core:column', { property: 'name', label: 'Name' })] });
    const item = element('link', 'core:action', { command: 'LinkOpportunityToCompany', label: 'Set company', schema: sceneSchema });
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Opportunities'>
                <StageAction element={item} slots={{}} />
                <RegisteredTable element={table} slots={{}} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    return fetched;
}

describe('an action on a selected row whose command has a field named like a row column', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('binds only the identifier the command acts on and keeps the other field editable', async () => {
        const fetched = renderOpportunities();
        fireEvent.click(await screen.findByText('Norsk deal'));
        await waitFor(() => expect((screen.getByRole('button', { name: 'Set company' }) as HTMLButtonElement).disabled).toBe(false));
        fireEvent.click(screen.getByRole('button', { name: 'Set company' }));
        const dialog = await screen.findByRole('dialog');

        expect(dialog.querySelector('[data-field="companyId"]')).not.toBeNull();
        expect(dialog.querySelector('[data-field="opportunityId"]')).toBeNull();
        const company = '7d1c8f3a-0000-4000-8000-0000000000c9';
        fireEvent.change(within(dialog).getByLabelText('Company', { exact: false }), { target: { value: company } });
        await act(async () => { fireEvent.click(dialog.querySelector('.scene-slice-dialog-submit, button[type="submit"]')!); });

        await waitFor(() => expect(fetched.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true));
        const [, init] = fetched.mock.calls.find(([, request]) => request?.method === 'POST')!;
        expect(JSON.parse(String(init?.body))).toEqual({ opportunityId: rows[1].opportunityId, companyId: company });
    });
});
