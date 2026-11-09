// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { FormGenerationMode } from '@cratis/scene.model';
import { RegisteredForm as StageCommandForm, commandResult } from './testing/commands';
import { StageDataProvider } from './stageData';
import { element } from './testElements';

const schema = JSON.stringify({ type: 'object', required: ['ownerId'], properties: { ownerId: { type: 'string' } } });
const hidden = (node: Element) => node.classList.contains('cratis-field-sr-only');

function submitRejected(result: Record<string, unknown>) {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult(result) }));
    render(
        <PrimeReactProvider>
            <StageDataProvider routes={{ commands: { AddPet: '/api/pets/add' }, queries: {} }} locale='en' locales={['en']} screen='Pets'>
                <StageCommandForm element={element('add-pet', 'Stage:commandForm', { command: 'AddPet', label: 'Add pet', schema, generationMode: FormGenerationMode.Auto })} />
            </StageDataProvider>
        </PrimeReactProvider>,
    );
    fireEvent.change(screen.getByLabelText('Owner id'), { target: { value: 'o-1' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add pet' }));
}

/** The alerts a person can see: a visually hidden announcement is not one. */
const visibleAlerts = () => screen.queryAllByRole('alert').filter(node => !hidden(node));

describe('a rejected command submit', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('shows what the command reported once, and Arc\'s generic exception notice only to assistive technology', async () => {
        submitRejected({ isSuccess: false, hasExceptions: true, exceptionMessages: ['The pet could not be stored'] });

        await waitFor(() => expect(screen.getAllByText('The pet could not be stored')).toHaveLength(1));
        expect(visibleAlerts().map(node => node.textContent)).toEqual(['The pet could not be stored']);
        const announced = screen.getByText('An unexpected error occurred. Please try again.');
        expect(hidden(announced)).toBe(true);
    });

    it('still shows that the command was not accepted when it reported no exception message', async () => {
        submitRejected({ isSuccess: false, hasExceptions: true, exceptionMessages: [] });

        await waitFor(() => expect(visibleAlerts().map(node => node.textContent)).toEqual(['“AddPet” was not accepted.']));
    });

    it('shows an authorization failure once', async () => {
        submitRejected({ isSuccess: false, isAuthorized: false, authorizationFailureReason: 'Only admins may add pets' });

        await waitFor(() => expect(screen.getAllByText('Only admins may add pets')).toHaveLength(1));
        expect(visibleAlerts()).toHaveLength(1);
    });

    it('shows each validation message once, the field one inline and the other below the form', async () => {
        submitRejected({ isSuccess: false, isValid: false, validationResults: [{ severity: 0, message: 'Owner id is wrong', members: ['ownerId'] }, { severity: 0, message: 'Pet is wrong', members: [] }] });

        await waitFor(() => expect(screen.getAllByText('Pet is wrong')).toHaveLength(1));
        expect(screen.getAllByText('Owner id is wrong').filter(node => !hidden(node))).toHaveLength(1);
        expect(visibleAlerts().map(node => node.textContent)).toEqual(['Pet is wrong']);
    });
});
