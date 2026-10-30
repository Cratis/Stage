// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, chai, describe, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm as StageCommandForm, commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { element } from './testElements';

chai.should();

describe('a Stage command form with camelCase validation members from Arc', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('should attach the message to the command member behind a Scene field alias', async () => {
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult({
            isSuccess: false, isValid: false,
            validationResults: [{ severity: 3, members: ['customerName'], message: 'Name is not unique', state: null }],
        }) }));
        const schema = JSON.stringify({ type: 'object', properties: { customerName: { type: 'string' }, title: { type: 'string' } } });
        render(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { RenameCustomer: '/api/rename-customer' }, queries: {} }} locale='en' locales={['en']} screen='Customers'>
                    <StageCommandForm element={element('rename', 'Stage:commandForm', {
                        command: 'RenameCustomer', schema, generationMode: FormGenerationMode.Auto,
                        fields: [{ name: 'displayName', sourceProperty: 'customerName', label: 'Customer name' }, { name: 'title', label: 'Title' }],
                    })} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        const name = screen.getByLabelText('Customer name');
        fireEvent.change(name, { target: { value: 'Acme' } });
        fireEvent.blur(name);
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));

        await waitFor(() => within(name.closest('[data-field]') as HTMLElement).getByText('Name is not unique', { selector: 'small' }).textContent!.should.equal('Name is not unique'));
        within(name.closest('[data-field]') as HTMLElement).getByText('Name is not unique', { selector: 'small' }).textContent!.should.equal('Name is not unique');
        (screen.getByLabelText('Title').getAttribute('aria-invalid') === 'true').should.equal(false);
    });
});
