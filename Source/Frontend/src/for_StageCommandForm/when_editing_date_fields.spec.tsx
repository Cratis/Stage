// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { DateOnly } from '@cratis/fundamentals';
import { registerCommand } from '@cratis/scene.components';
import { StageCommandForm } from '../../Runtime/src/StageCommandForm';
import { StageDataProvider } from '../../Runtime/src/stageData';
import { element } from '../../Runtime/src/testElements';
import { commandResult } from '../../Runtime/src/testing/commands';
import { queryResult, registerTestQueries } from '../../Runtime/src/testing/queries';
import { registerStageCommands } from '../host/commandRegistration';

chai.should();
const schema = JSON.stringify({ properties: { birthDate: { type: 'string', format: 'date' } }, required: ['birthDate'] });
const routes = { commands: { SaveBirthDate: '/api/birth-date' }, queries: { GetBirthDate: '/api/birth-date/draft' }, commandSchemas: { SaveBirthDate: schema } };

function registerPlainSetterCommand(type: typeof DateOnly | typeof Date) {
    class SaveBirthDate extends Command {
        readonly route = 'api/birth-date';
        readonly propertyDescriptors = [new PropertyDescriptor('birthDate', type, true)];
        get requestParameters(): string[] { return []; }
        private _birthDate: unknown;
        get birthDate() { return this._birthDate; }
        set birthDate(value: unknown) {
            this._birthDate = value;
            this.propertyChanged('birthDate');
        }
        constructor() { super(Object, false); }
    }
    registerCommand('SaveBirthDate', SaveBirthDate);
}

function view(parameters: Record<string, string> = {}, properties: Record<string, unknown> = {}, register = () => registerStageCommands(routes)) {
    register();
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
        fireEvent.click(screen.getByRole('button', { name: 'Birth date' }));

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
        fireEvent.click(screen.getByRole('button', { name: 'Birth date' }));

        await waitFor(() => submitted(fetched).should.deep.equal({ birthDate: '2020-01-01' }));
    });

    it.each([{ type: DateOnly, name: 'DateOnly' }, { type: Date, name: 'Date' }])('should submit the picked calendar day through a plain-setter $name proxy', async ({ type }) => {
        const fetched = vi.fn().mockResolvedValue(new Response(JSON.stringify(commandResult())));
        vi.stubGlobal('fetch', fetched);
        view({ birthDate: '2020-01-02' }, {}, () => registerPlainSetterCommand(type));

        const day = screen.getByRole('spinbutton', { name: /day/i });
        fireEvent.keyDown(day, { key: 'ArrowDown' });
        day.textContent!.should.equal('1');
        fireEvent.click(screen.getByRole('button', { name: 'Birth date' }));

        await waitFor(() => submitted(fetched).should.deep.equal({ birthDate: '2020-01-01' }));
    });

    it('should keep a cleared date null through a plain-setter proxy', async () => {
        const fetched = vi.fn().mockResolvedValue(new Response(JSON.stringify(commandResult())));
        vi.stubGlobal('fetch', fetched);
        const optionalSchema = JSON.stringify({ properties: { birthDate: { type: 'string', format: 'date' } }, required: [] });
        view({ birthDate: '2020-01-01' }, { schema: optionalSchema }, () => registerPlainSetterCommand(DateOnly));

        for (const segment of screen.getAllByRole('spinbutton')) {
            const digits = segment.textContent!.length;
            for (let digit = 0; digit < digits; digit++) fireEvent.keyDown(segment, { key: 'Backspace' });
        }
        fireEvent.click(screen.getByRole('button', { name: 'Birth date' }));

        await waitFor(() => submitted(fetched).should.deep.equal({ birthDate: null }));
    });

    it('should include time selection for a date-time field', () => {
        const instantSchema = JSON.stringify({ properties: { appointment: { type: 'string', format: 'date-time' } } });
        view({}, { schema: instantSchema, fields: [{ name: 'appointment', label: 'Appointment' }] });

        chai.assert.exists(screen.getByRole('spinbutton', { name: /hour/i }));
        chai.assert.exists(screen.getByRole('spinbutton', { name: /minute/i }));
    });
});
