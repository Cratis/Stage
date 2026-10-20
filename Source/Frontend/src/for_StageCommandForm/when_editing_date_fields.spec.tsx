// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { StageCommandForm } from '../../Runtime/src/StageCommandForm';
import { StageDataProvider } from '../../Runtime/src/stageData';
import { element } from '../../Runtime/src/testElements';
import { commandResult } from '../../Runtime/src/testing/commands';
import { queryResult, registerTestQueries } from '../../Runtime/src/testing/queries';
import { registerStageCommands } from '../host/commandRegistration';

chai.should();
const schema = JSON.stringify({ properties: { birthDate: { type: 'string', format: 'date' } }, required: ['birthDate'] });
const routes = { commands: { SaveBirthDate: '/api/birth-date' }, queries: { GetBirthDate: '/api/birth-date/draft' }, commandSchemas: { SaveBirthDate: schema } };

function view(parameters: Record<string, string> = {}, properties: Record<string, unknown> = {}) {
    registerStageCommands(routes);
    return render(<PrimeReactProvider><StageDataProvider routes={routes} parameters={parameters} locale='en' locales={['en']} screen='Dates'>
        <StageCommandForm element={element('date-form', 'Stage:commandForm', {
            command: 'SaveBirthDate', label: 'Birth date', schema, generationMode: FormGenerationMode.Auto,
            fields: [{ name: 'birthDate', label: 'Birth date' }], ...properties,
        })} />
    </StageDataProvider></PrimeReactProvider>);
}

function submitted(fetched: ReturnType<typeof vi.fn>) {
    const call = fetched.mock.calls.find(([url]) => String(url).endsWith('/api/birth-date'));
    return call ? JSON.parse(String((call[1] as RequestInit).body)) : undefined;
}

describe('when editing date-only command fields', () => {
    afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

    it('should display and submit a calendar date supplied by the screen without shifting it', async () => {
        const fetched = vi.fn().mockResolvedValue(new Response(JSON.stringify(commandResult())));
        vi.stubGlobal('fetch', fetched);
        view({ birthDate: '2020-01-01' });

        screen.getByRole('spinbutton', { name: /year/i }).textContent!.should.equal('2020');
        screen.getByRole('spinbutton', { name: /month/i }).textContent!.should.equal('1');
        screen.getByRole('spinbutton', { name: /day/i }).textContent!.should.equal('1');
        fireEvent.click(screen.getByRole('button', { name: 'Execute Birth date' }));

        await waitFor(() => submitted(fetched).should.deep.equal({ birthDate: '2020-01-01' }));
    });

    it('should display a populated calendar date and submit the day picked instead', async () => {
        const fetched = vi.fn((url: RequestInfo | URL) => Promise.resolve(new Response(JSON.stringify(String(url).includes('/draft')
            ? queryResult({ data: [{ birthDate: '2020-01-02' }] }) : commandResult()))));
        vi.stubGlobal('fetch', fetched);
        registerTestQueries(routes.queries);
        view({}, { populateSource: { queryName: 'GetBirthDate' } });

        await waitFor(() => screen.getByRole('spinbutton', { name: /year/i }).textContent!.should.equal('2020'));
        const day = screen.getByRole('spinbutton', { name: /day/i });
        day.textContent!.should.equal('2');
        fireEvent.keyDown(day, { key: 'ArrowDown' });
        day.textContent!.should.equal('1');
        fireEvent.click(screen.getByRole('button', { name: 'Execute Birth date' }));

        await waitFor(() => submitted(fetched).should.deep.equal({ birthDate: '2020-01-01' }));
    });

    it('should include time selection for a date-time field', () => {
        const instantSchema = JSON.stringify({ properties: { appointment: { type: 'string', format: 'date-time' } } });
        view({}, { schema: instantSchema, fields: [{ name: 'appointment', label: 'Appointment' }] });

        chai.assert.exists(screen.getByRole('spinbutton', { name: /hour/i }));
        chai.assert.exists(screen.getByRole('spinbutton', { name: /minute/i }));
    });
});
