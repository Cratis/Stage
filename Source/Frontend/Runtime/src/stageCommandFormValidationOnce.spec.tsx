// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm as StageCommandForm, commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { unboundValidationMessages } from './StageCommandForm';
import { element } from './testElements';

const schema = JSON.stringify({ type: 'object', required: ['ownerId'], properties: { ownerId: { type: 'string' } } });

describe('validation messages of a command form', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('shows a message about a field once, labelled with the field label, and one about no field under the form', async () => {
        const validationResults = [
            { severity: 0, message: 'Owner id is required', members: ['ownerId'] },
            { severity: 0, message: 'The pet needs an owner', members: [] },
        ];
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult({ isSuccess: false, isValid: false, validationResults }) }));

        render(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { AddPet: '/api/pets/add' }, queries: {} }} locale='en' locales={['en']} screen='Pets'>
                    <StageCommandForm element={element('add-pet', 'Stage:commandForm', { command: 'AddPet', label: 'Add pet', schema, generationMode: FormGenerationMode.Auto })} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        fireEvent.change(screen.getByLabelText('Owner id'), { target: { value: 'o-1' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Add pet' }));

        await waitFor(() => expect(screen.getAllByText('The pet needs an owner')).toHaveLength(1));
        // Arc also gives the field a screen-reader-only copy of its error; that is not a second visible message.
        const visible = screen.getAllByText('Owner id is required').filter(node => !node.classList.contains('cratis-field-sr-only'));
        expect(visible).toHaveLength(1);
        expect(visible[0].closest('.stage-command-form__field')).not.toBeNull();
        expect(screen.queryByText(/ownerId is required/)).toBeNull();
    });

    it('shows the first message about a field inline and every further one below the form', async () => {
        const validationResults = [
            { severity: 0, message: 'Owner id is required', members: ['ownerId'] },
            { severity: 0, message: 'Owner id is unknown', members: ['ownerId.Value'] },
        ];
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult({ isSuccess: false, isValid: false, validationResults }) }));

        render(
            <PrimeReactProvider>
                <StageDataProvider routes={{ commands: { AddPet: '/api/pets/add' }, queries: {} }} locale='en' locales={['en']} screen='Pets'>
                    <StageCommandForm element={element('add-pet', 'Stage:commandForm', { command: 'AddPet', label: 'Add pet', schema, generationMode: FormGenerationMode.Auto })} />
                </StageDataProvider>
            </PrimeReactProvider>,
        );
        fireEvent.change(screen.getByLabelText('Owner id'), { target: { value: 'o-1' } });
        fireEvent.click(screen.getByRole('button', { name: 'Execute Add pet' }));

        await waitFor(() => expect(screen.getAllByText('Owner id is unknown').filter(node => node.classList.contains('stage-form-error'))).toHaveLength(1));
        const inline = screen.getAllByText('Owner id is required').filter(node => !node.classList.contains('cratis-field-sr-only'));
        expect(inline).toHaveLength(1);
        expect(inline[0].classList.contains('stage-form-error')).toBe(false);
    });

    it('keeps only the messages that are not about a rendered field', () => {
        const fields = [{ name: 'ownerId' }, { name: 'address', sourceProperty: 'shipTo' }];

        expect(unboundValidationMessages(fields, [
            { message: 'a', members: ['ownerId'] },
            { message: 'b', members: ['shipTo.City'] },
            { message: 'c', members: ['other'] },
            { message: 'c', members: undefined },
            { message: 'd', members: ['ownerId'] },
        ])).toEqual(['c', 'd']);
    });
});
