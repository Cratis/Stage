// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm, commandResult } from './testing/commands';
import { queryResult, registerTestQueries } from './testing/queries';
import { StageDataProvider, useStageData } from './stageData';
import { StageScreenTables } from './stageScreenTables';
import { sessionIdentifierPrefix } from './stageSession';
import { element } from './testElements';

const routes = { commands: { RecoverReference: '/api/recover-reference' }, queries: { RecoveryOrganisations: '/api/recovery-organisations' } };
const schema = JSON.stringify({ type: 'object', required: ['opportunityId', 'organisationId'], properties: {
    opportunityId: { type: 'string', format: 'uuid', 'x-concept': 'RecoveryOpportunityId', 'x-identity': true },
    organisationId: { type: 'string', format: 'uuid', 'x-concept': 'RecoveryOrganisationId', 'x-reference': { query: 'RecoveryOrganisations', value: 'organisationId', label: 'name' } },
} });
const form = element('recovery', 'Stage:commandForm', { command: 'RecoverReference', label: 'Recover reference', schema, generationMode: FormGenerationMode.Manual, fields: [{ name: 'organisationId', label: 'Organisation' }] });

function SelectReferences() {
    const data = useStageData();
    return <>
        <button onClick={() => data.selectRow('own-table', { organisationId: 'own' })}>Select own reference</button>
        <button onClick={() => data.selectRow('other-table', { organisationId: 'other' })}>Select other reference</button>
    </>;
}

function view(parameters: Record<string, string> = {}, properties: Record<string, unknown> = {}) {
    registerTestQueries(routes.queries);
    const ownScreen = element('recovery-screen', 'core:stack', {}, { children: [element('own-table', 'core:table')] });
    return render(<PrimeReactProvider><StageDataProvider routes={routes} parameters={parameters} locale='en' locales={['en']} screen='Recovery'>
        <SelectReferences />
        <StageScreenTables element={ownScreen}>
            <RegisteredForm element={{ ...form, properties: { ...form.properties, ...properties } }} />
        </StageScreenTables>
    </StageDataProvider></PrimeReactProvider>);
}

async function submittedReference(fetched: ReturnType<typeof fetchedQuery>) {
    fireEvent.click(screen.getByRole('button', { name: 'Recover reference' }));
    await waitFor(() => expect(fetched.mock.calls.some(([url]) => String(url).includes('/api/recover-reference'))).toBe(true));
    const posted = fetched.mock.calls.find(([url]) => String(url).includes('/api/recover-reference'))!;
    return JSON.parse(String(posted[1]?.body));
}

function holdReference() {
    globalThis.localStorage.setItem(`${sessionIdentifierPrefix}RecoveryOrganisationId`, 'held');
}

function optionalSchema() {
    const optional = JSON.parse(schema);
    optional.required = ['opportunityId'];
    return JSON.stringify(optional);
}

function fetchedQuery() {
    const fetched = vi.fn((url: RequestInfo | URL, _init?: RequestInit) => Promise.resolve({ ok: true, json: async () =>
        String(url).includes('recovery-organisations') ? queryResult({ data: [{ organisationId: 'o1', name: 'Acme' }, { organisationId: 'o2', name: 'Nordic' }] }) : commandResult() }));
    vi.stubGlobal('fetch', fetched);
    return fetched;
}

describe('reference pickers on the recovery form pipeline', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); globalThis.localStorage?.clear(); });

    it('offers modeled reference labels and submits the key while supplying its own identity', async () => {
        const fetched = fetchedQuery();
        view();
        await screen.findByRole('option', { name: 'Nordic' });
        fireEvent.change(screen.getByRole('combobox', { name: 'Organisation' }), { target: { value: 'o2' } });
        expect(screen.queryByLabelText('Opportunity id')).toBeNull();
        const body = await submittedReference(fetched);
        expect(body.organisationId).toBe('o2');
        expect(body.opportunityId).toMatch(/^[0-9a-f-]{36}$/i);
    });

    it('hides an address-supplied reference without a choice query and gives the address precedence', async () => {
        const fetched = fetchedQuery();
        holdReference();
        view({ organisationId: 'address' }, { subjectValues: { organisationId: 'bound' } });
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        expect(fetched).not.toHaveBeenCalled();
        expect((await submittedReference(fetched)).organisationId).toBe('address');
    });

    it('hides a held-session reference without creating or querying another identity', async () => {
        const fetched = fetchedQuery();
        holdReference();
        view();
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        expect(fetched).not.toHaveBeenCalled();
        expect((await submittedReference(fetched)).organisationId).toBe('held');
    });

    it('offers an unsupplied optional reference without generating its identity', async () => {
        const fetched = fetchedQuery();
        view({}, { schema: optionalSchema() });
        await screen.findByRole('option', { name: 'Nordic' });
        const picker = screen.getByRole('combobox', { name: 'Organisation' }) as HTMLSelectElement;
        expect(picker.required).toBe(false);
        expect(globalThis.localStorage.getItem(`${sessionIdentifierPrefix}RecoveryOrganisationId`)).toBeNull();
        fireEvent.change(picker, { target: { value: 'o2' } });
        expect((await submittedReference(fetched)).organisationId).toBe('o2');
    });

    it('also hides a supplied optional reference', async () => {
        const fetched = fetchedQuery();
        holdReference();
        view({}, { schema: optionalSchema() });
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        expect(fetched).not.toHaveBeenCalled();
        expect((await submittedReference(fetched)).organisationId).toBe('held');
    });

    it('uses only the current screen selection to supply a reference without resetting ordinary edits', async () => {
        const fetched = fetchedQuery();
        const ordinary = JSON.parse(schema);
        ordinary.properties.notes = { type: 'string' };
        view({}, { schema: JSON.stringify(ordinary), fields: [
            { name: 'organisationId', label: 'Organisation' }, { name: 'notes', label: 'Notes' },
        ] });
        await screen.findByRole('option', { name: 'Acme' });
        fireEvent.change(screen.getByRole('textbox', { name: 'Notes' }), { target: { value: 'Keep this edit' } });
        fireEvent.click(screen.getByRole('button', { name: 'Select other reference' }));
        expect(screen.getByRole('combobox', { name: 'Organisation' })).toBeTruthy();
        fireEvent.click(screen.getByRole('button', { name: 'Select own reference' }));
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        expect((screen.getByRole('textbox', { name: 'Notes' }) as HTMLInputElement).value).toBe('Keep this edit');
        const body = await submittedReference(fetched);
        expect(body.organisationId).toBe('own');
        expect(body.notes).toBe('Keep this edit');
    });

    it('ignores a selection for an explicit screen subject', async () => {
        const fetched = fetchedQuery();
        view({}, { actionSubject: 'screen' });
        await screen.findByRole('option', { name: 'Acme' });
        fireEvent.click(screen.getByRole('button', { name: 'Select own reference' }));
        fireEvent.change(screen.getByRole('combobox', { name: 'Organisation' }), { target: { value: 'o2' } });
        expect((await submittedReference(fetched)).organisationId).toBe('o2');
    });

    it('keeps item-bound inputs ahead of held-session reference values without querying choices', async () => {
        const fetched = fetchedQuery();
        holdReference();
        view({}, { actionSubject: 'item', subjectValues: { organisationId: 'bound' } });
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        expect(fetched).not.toHaveBeenCalled();
        expect((await submittedReference(fetched)).organisationId).toBe('bound');
    });

    it('hides an explicitly bound reference even when neither address nor session supplies it', async () => {
        const fetched = fetchedQuery();
        view({}, { actionSubject: 'item', subjectValues: { organisationId: 'bound' } });
        expect(screen.queryByLabelText('Organisation')).toBeNull();
        expect(fetched).not.toHaveBeenCalled();
        expect((await submittedReference(fetched)).organisationId).toBe('bound');
    });

    it('leaves ordinary fields editable while a reference is supplied', async () => {
        const fetched = fetchedQuery();
        const ordinary = JSON.parse(schema);
        ordinary.properties.notes = { type: 'string' };
        view({ organisationId: 'address' }, { schema: JSON.stringify(ordinary), fields: [
            { name: 'organisationId', label: 'Organisation' }, { name: 'notes', label: 'Notes' },
        ] });
        fireEvent.change(screen.getByRole('textbox', { name: 'Notes' }), { target: { value: 'Keep ordinary inputs' } });
        expect(fetched).not.toHaveBeenCalled();
        const body = await submittedReference(fetched);
        expect(body.notes).toBe('Keep ordinary inputs');
        expect(body.organisationId).toBe('address');
    });
});
