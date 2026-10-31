// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { BindingSourceKind, FormGenerationMode, InteractionActionKind } from '@cratis/scene.model';
import type { NavigateAction } from '@cratis/scene.model';
import { createBrowserDispatcher, InteractionScope } from '@cratis/scene.react';
import { commandResult } from '../../testing/commands';
import { StageCommandForm, StageDialogContext } from '../../StageCommandForm';
import { registerStageCommands } from '../../../../src/host/commandRegistration';
import { StageDataProvider } from '../../stageData';
import { element } from '../../testElements';
import { stageResponseContracts } from '../../stageResponseContracts';

const returnedId = '49f687cc-9e87-494e-8c70-8f779c919e17';
const schema = JSON.stringify({ type: 'object', required: ['sku'], properties: { sku: { type: 'string' } } });
const routes = { commands: { PlaceOrder: '/api/order/place' }, queries: {} };
const onSubmit: NavigateAction[] = [{ kind: InteractionActionKind.Navigate, screen: 'Details', arguments: [{ name: 'orderId', value: { kind: BindingSourceKind.CommandResponse, path: '' } }] }];
const responseContracts = stageResponseContracts({
    commands: { PlaceOrder: { kind: 'value', type: { kind: 'string' } } },
    commandInputs: {},
    screenInputs: { Details: { orderId: { kind: 'string' } } },
});

function form(result: Record<string, unknown>) {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult(result) }));
    const navigate = vi.fn();
    const closeDialog = vi.fn();
    registerStageCommands({ ...routes, commandSchemas: { PlaceOrder: schema }, commandResponseKinds: { PlaceOrder: 'string' } });
    render(<PrimeReactProvider>
        <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Create'>
            <StageDialogContext.Provider value={closeDialog}>
                <InteractionScope dispatcher={createBrowserDispatcher({ navigate })} context={{ resolve: () => 'request-or-selected-row', responseContracts }} attachments={[]}>
                    <StageCommandForm element={element('place', 'Stage:commandForm', { command: 'PlaceOrder', schema, generationMode: FormGenerationMode.Auto, fields: [{ name: 'sku', label: 'Product' }], navigateToScreen: 'Basket', onSubmit })} />
                </InteractionScope>
            </StageDialogContext.Provider>
        </StageDataProvider>
    </PrimeReactProvider>);
    return { navigate, closeDialog };
}

describe('a command form whose continuation navigates', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '/'); globalThis.localStorage?.clear(); });

    it('leaves the destination to the continuation instead of navigating to the action destination', async () => {
        const { navigate, closeDialog } = form({ response: returnedId });
        fireEvent.change(screen.getByLabelText('Product'), { target: { value: 'ACM-1001' } });
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
        await waitFor(() => expect(navigate).toHaveBeenCalledExactlyOnceWith('Details', { orderId: returnedId }));
        await waitFor(() => expect(closeDialog).toHaveBeenCalledOnce());
        expect(globalThis.location.hash).not.toContain('Basket');
    });
});
