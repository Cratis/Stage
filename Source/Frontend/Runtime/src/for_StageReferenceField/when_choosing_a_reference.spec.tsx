// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm, commandResult } from '../testing/commands';
import { queryResult, registerTestQueries } from '../testing/queries';
import { StageDataProvider, useStageData } from '../stageData';
import { schemaPropertiesOf } from '../stageFormSchema';
import { element } from '../testElements';

chai.should();

const reference = { query: 'AllOrganisations', value: 'organisationId', label: 'name' };
const schema = JSON.stringify({ type: 'object', required: ['organisationId'], properties: {
    organisationId: { type: 'string', format: 'uuid', 'x-concept': 'OrganisationId', 'x-reference': reference },
} });
const routes = { commands: { RegisterOpportunity: '/api/opportunity' }, queries: { AllOrganisations: '/api/organisations' } };
const rows = [{ organisationId: 'o1', name: 'Acme' }, { organisationId: 'o2', name: 'Nordic' }];
const form = element('opportunity', 'Stage:commandForm', { command: 'RegisterOpportunity', label: 'Register', schema, generationMode: FormGenerationMode.Auto });

function SelectRow() {
    const data = useStageData();
    return <button onClick={() => data.selectRow('organisations', rows[0])}>Select row</button>;
}

function content(parameters: Record<string, string>, ready: boolean, queries: Record<string, string>) {
    return <PrimeReactProvider><StageDataProvider routes={{ ...routes, queries }} routesReady={ready} parameters={parameters} locale='en' locales={['en']} screen='Opportunity'>
        <SelectRow /><RegisteredForm element={form} />
    </StageDataProvider></PrimeReactProvider>;
}

function view(parameters: Record<string, string> = {}, ready = true, queries: Record<string, string> = routes.queries) {
    registerTestQueries(queries);
    return render(content(parameters, ready, queries));
}

function fetchedQuery(data: Record<string, unknown>[] = rows) {
    const fetched = vi.fn((url: RequestInfo | URL, _init?: RequestInit) => Promise.resolve({ ok: true, json: async () =>
        String(url).includes('organisations') ? queryResult({ data }) : commandResult() }));
    vi.stubGlobal('fetch', fetched);
    return fetched;
}

describe('when choosing a reference', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); globalThis.localStorage?.clear(); });

    it('should read the shared schema annotation without a planned route', () => {
        schemaPropertiesOf(schema)[0].reference!.should.deep.equal(reference);
    });

    it('should show names from the registered query and submit only the selected key', async () => {
        const fetched = fetchedQuery();
        view();
        await screen.findByRole('option', { name: 'Nordic' });
        fireEvent.change(screen.getByRole('combobox', { name: 'Organisation id' }), { target: { value: 'o2' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Register' }));
        await waitFor(() => fetched.mock.calls.some(([url]) => String(url).includes('/api/opportunity')).should.equal(true));
        const posted = fetched.mock.calls.find(([url]) => String(url).includes('/api/opportunity'))!;
        JSON.parse(String(posted[1]?.body)).organisationId.should.equal('o2');
    });

    it('should filter names without replacing the selected key', async () => {
        fetchedQuery();
        view();
        await screen.findByRole('option', { name: 'Nordic' });
        fireEvent.change(screen.getByRole('combobox', { name: 'Organisation id' }), { target: { value: 'o2' } });
        fireEvent.change(screen.getByRole('searchbox', { name: 'Filter Organisation id' }), { target: { value: 'acm' } });
        screen.getByRole('option', { name: 'Acme' });
        screen.getByRole('option', { name: 'Nordic' });
        (screen.getByRole('combobox') as HTMLSelectElement).value.should.equal('o2');
        fireEvent.change(screen.getByRole('combobox'), { target: { value: '' } });
        (screen.getByRole('combobox') as HTMLSelectElement).value.should.equal('');
        (screen.queryByRole('option', { name: 'Nordic' }) === null).should.equal(true);
    });

    it('should show the key if the chosen label is absent', async () => {
        fetchedQuery([{ organisationId: 'o3' }]);
        view();
        await screen.findByRole('option', { name: 'o3' });
    });

    it('should keep a text input without querying until registered routes become ready', async () => {
        const fetched = fetchedQuery();
        const result = view({}, false);
        screen.getByRole('textbox', { name: 'Organisation id' });
        fetched.mock.calls.length.should.equal(0);
        result.rerender(content({}, true, routes.queries));
        await screen.findByRole('option', { name: 'Acme' });
    });

    it('should keep a text input if the query has no registered route', () => {
        const fetched = fetchedQuery();
        view({}, true, {});
        screen.getByRole('textbox', { name: 'Organisation id' });
        fetched.mock.calls.length.should.equal(0);
    });

    it('should show query errors and allow the key to be entered instead', async () => {
        vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Offline')));
        view();
        await screen.findByRole('alert');
        screen.getByRole('textbox', { name: 'Organisation id' });
    });

    it('should show loading and an empty result without inventing a choice', async () => {
        let finish!: (value: unknown) => void;
        vi.stubGlobal('fetch', vi.fn(() => new Promise(resolve => { finish = resolve; })));
        view();
        await screen.findByRole('status');
        await act(async () => finish({ ok: true, json: async () => queryResult() }));
        await screen.findByText('No choices available');
        screen.getAllByRole('option').length.should.equal(1);
    });

    it('should omit a reference already supplied by the address', () => {
        const fetched = fetchedQuery();
        view({ organisationId: 'o1' });
        (screen.queryByLabelText('Organisation id') === null).should.equal(true);
        fetched.mock.calls.length.should.equal(0);
    });

    it('should omit a reference supplied by the selected row', async () => {
        fetchedQuery();
        view();
        await screen.findByRole('option', { name: 'Acme' });
        fireEvent.click(screen.getByRole('button', { name: 'Select row' }));
        (screen.queryByLabelText('Organisation id') === null).should.equal(true);
    });
});
