// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { BindingSourceKind, FormGenerationMode, InteractionActionKind } from '@cratis/scene.model';
import type { InteractionAction, NavigateAction } from '@cratis/scene.model';
import { createBrowserDispatcher } from '@cratis/scene.react';
import { InteractionScope } from '@cratis/scene.react';
import type { ResponseValidationContracts } from '@cratis/scene.engine';
import { commandResult } from '../../testing/commands';
import { StageCommandForm } from '../../StageCommandForm';
import { registerStageCommands } from '../../../../src/host/commandRegistration';
import { StageDataProvider } from '../../stageData';
import { element } from '../../testElements';

const returnedId = '49f687cc-9e87-494e-8c70-8f779c919e17';
const schema = JSON.stringify({ type: 'object', required: ['sku'], properties: { sku: { type: 'string' } } });
const routes = { commands: { PlaceOrder: '/api/order/place' }, queries: {} };
const onSubmit: NavigateAction[] = [{ kind: InteractionActionKind.Navigate, screen: 'Details', arguments: [{ name: 'orderId', value: { kind: BindingSourceKind.CommandResponse, path: '' } }] }];
const contracts: ResponseValidationContracts = {
    commandResponse: () => ({ kind: 'value', type: { kind: 'string' } }),
    targetType: () => ({ kind: 'string' }),
};

function form(result: Record<string, unknown>, responseContracts: ResponseValidationContracts = contracts, actions: InteractionAction[] = onSubmit) {
    const fetch = vi.fn().mockResolvedValue({ ok: true, json: async () => commandResult(result) });
    vi.stubGlobal('fetch', fetch);
    const navigate = vi.fn();
    const findings = vi.fn();
    const responseContract = responseContracts.commandResponse('PlaceOrder');
    const kind = responseContract?.kind === 'value' ? responseContract.type.kind : 'object';
    registerStageCommands({ ...routes, commandSchemas: { PlaceOrder: schema }, commandResponseKinds: { PlaceOrder: kind } });
    render(<PrimeReactProvider>
        <StageDataProvider routes={routes} locale='en' locales={['en']} screen='Create'>
            <InteractionScope dispatcher={createBrowserDispatcher({ navigate })} context={{ resolve: () => 'request-or-selected-row', responseContracts }} attachments={[]} onFindings={findings}>
                <StageCommandForm element={element('place', 'Stage:commandForm', { command: 'PlaceOrder', schema, generationMode: FormGenerationMode.Auto, fields: [{ name: 'sku', label: 'Product' }], onSubmit: actions })} />
            </InteractionScope>
        </StageDataProvider>
    </PrimeReactProvider>);
    return { fetch, navigate, findings };
}

function submit() {
    fireEvent.change(screen.getByLabelText('Product'), { target: { value: 'ACM-1001' } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
}

describe('a native command form response continuation', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '/'); globalThis.localStorage?.clear(); });

    it('navigates once with the scalar response, never a request field or row value', async () => {
        const { fetch, navigate } = form({ response: returnedId });
        submit();
        await waitFor(() => expect(navigate).toHaveBeenCalledExactlyOnceWith('Details', { orderId: returnedId }));
        expect(JSON.parse(fetch.mock.calls[0][1].body)).toEqual({ sku: 'ACM-1001' });
    });

    it('projects the named response field', async () => {
        const actions: NavigateAction[] = [{ kind: InteractionActionKind.Navigate, screen: 'Details', arguments: [{ name: 'orderId', value: { kind: BindingSourceKind.CommandResponse, path: 'orderId' } }] }];
        const named: ResponseValidationContracts = { ...contracts, commandResponse: () => ({ kind: 'value', type: { kind: 'object', properties: { orderId: { kind: 'string' } } } }) };
        const { navigate } = form({ response: { orderId: returnedId } }, named, actions);
        submit();
        await waitFor(() => expect(navigate).toHaveBeenCalledExactlyOnceWith('Details', { orderId: returnedId }));
    });

    it.each([undefined, null, 42])('reports a missing or incompatible response (%s), without falling back to data', async response => {
        const { navigate, findings } = form({ response });
        submit();
        await waitFor(() => expect(findings).toHaveBeenCalledOnce());
        expect(navigate).not.toHaveBeenCalled();
        expect(findings.mock.calls[0][0][0].kind).toBe('invalidResponseBinding');
    });

    it('never reuses a previous successful response on a later submission', async () => {
        const { fetch, navigate, findings } = form({ response: returnedId });
        submit();
        await waitFor(() => expect(navigate).toHaveBeenCalledOnce());
        fetch.mockResolvedValueOnce({ ok: true, json: async () => commandResult({ response: null }) });
        await waitFor(() => expect(screen.getByRole('button', { name: 'Submit' }).hasAttribute('disabled')).toBe(false));
        fireEvent.click(screen.getByRole('button', { name: 'Submit' }));
        await waitFor(() => expect(findings).toHaveBeenCalledOnce());
        expect(fetch).toHaveBeenCalledTimes(2);
        expect(navigate).toHaveBeenCalledOnce();
    });

    it('refuses a response binding when its response contract is unavailable', async () => {
        const { fetch } = form({ response: returnedId }, { ...contracts, commandResponse: () => undefined });
        expect((await screen.findByRole('alert')).textContent).toContain('available, compatible source and destination');
        expect(fetch).not.toHaveBeenCalled();
    });

    it('refuses an incompatible destination before submitting the command', async () => {
        const { fetch, navigate } = form({ response: returnedId }, { ...contracts, targetType: () => ({ kind: 'number' }) });
        expect((await screen.findByRole('alert')).textContent).toContain('compatible');
        expect(screen.queryByRole('button', { name: 'Submit' })).toBeNull();
        expect(fetch).not.toHaveBeenCalled();
        expect(navigate).not.toHaveBeenCalled();
    });

    it.each([
        { isSuccess: false, isValid: false, validationResults: [{ members: [], message: 'Rejected' }] },
        { isSuccess: false, isAuthorized: false, authorizationFailureReason: 'Denied' },
        { isSuccess: false, hasExceptions: true, exceptionMessages: ['Failed'] },
    ])('does not continue a rejected command (%j)', async rejected => {
        const { fetch, navigate, findings } = form({ ...rejected, response: returnedId });
        submit();
        await waitFor(() => expect(fetch).toHaveBeenCalledOnce());
        await screen.findAllByRole('alert');
        expect(navigate).not.toHaveBeenCalled();
        expect(findings).not.toHaveBeenCalled();
    });
});
