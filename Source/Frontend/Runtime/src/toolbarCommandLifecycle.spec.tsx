// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect } from 'react';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core';
import { registerStageCommands } from '../../src/host/commandRegistration';
import { stageComponents } from './stageComponents';
import { StageDataProvider, useStageData } from './stageData';
import { element } from './testElements';
import { commandResult } from './testing/commands';

const StageToolbarItem = stageComponents['core:toolbarItem'];
const schema = JSON.stringify({ type: 'object', required: ['name'], properties: { name: { type: 'string' } } });
const routes = { commands: { RegisterProject: '/api/projects/register' }, queries: {}, commandSchemas: { RegisterProject: schema } };
const response = () => ({ ok: true, json: async () => commandResult() } as Response);

function StateSource({ value }: { value?: string }) {
    const { selectRow } = useStageData();
    useEffect(() => selectRow('owners', { provider: value }), [value, selectRow]);
    return null;
}

function show(provided = false) {
    const metadata = provided ? JSON.stringify({ type: 'object', required: ['name', 'ownerId'], properties: { name: { type: 'string' }, ownerId: { type: 'string' } } }) : schema;
    const given = { ...routes, commandSchemas: { RegisterProject: metadata } };
    registerStageCommands(given);
    const content = (value?: string) => <PrimeReactProvider>
        <StageDataProvider routes={given} locale='en' locales={['en']} screen='Projects'>
            <StateSource value={value} />
            <StageToolbarItem element={element('register', 'core:toolbarItem', {
                name: 'register', kind: 'Action', target: 'RegisterProject', label: 'Register', schema: metadata,
                ...(provided ? {
                    parameters: { ownerId: { path: 'provider' }, intruder: { path: 'provider' } },
                } : {}),
            })} slots={{}} />
        </StageDataProvider>
    </PrimeReactProvider>;
    const view = render(content('owner-1'));
    return { ...view, source: (value?: string) => view.rerender(content(value)) };
}

async function open(name = 'Name') {
    fireEvent.click(screen.getByRole('button', { name: /^Register$/ }));
    const dialog = await screen.findByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText(name, { exact: false }), { target: { value: 'Fjordline' } });
    return dialog;
}

describe('the native toolbar command lifecycle', () => {
    afterEach(() => { vi.unstubAllGlobals(); globalThis.localStorage?.clear(); });

    it('does not post again when the submit button is clicked while the first submit is pending', async () => {
        let complete!: (response: Response) => void;
        const fetched = vi.fn(() => new Promise<Response>(resolve => { complete = resolve; }));
        vi.stubGlobal('fetch', fetched);
        show();
        const dialog = await open();
        fireEvent.click(within(dialog).getByRole('button', { name: /execute/i }));
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        try {
            fireEvent.click(within(dialog).getByRole('button', { name: /execute/i }));
            await act(async () => { await Promise.resolve(); });
            expect(fetched).toHaveBeenCalledTimes(1);
        } finally {
            await act(async () => { complete(response()); });
        }
    });

    it('maps toolbar binding dictionaries to hidden schema members without leaking nonmembers', async () => {
        const fetched = vi.fn().mockResolvedValue(response());
        vi.stubGlobal('fetch', fetched);
        show(true);
        const dialog = await open();
        expect(within(dialog).getAllByRole('textbox')).toHaveLength(1);
        fireEvent.submit(dialog.querySelector('form')!);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        expect(JSON.parse(fetched.mock.calls[0][1].body)).toEqual({ name: 'Fjordline', ownerId: 'owner-1' });
    });

    it('clears a supplied hidden value when its source disappears and allows a corrected retry', async () => {
        const fetched = vi.fn().mockResolvedValue(response());
        vi.stubGlobal('fetch', fetched);
        const view = show(true);
        const dialog = await open();
        view.source(undefined);
        fireEvent.submit(dialog.querySelector('form')!);
        await act(async () => { await Promise.resolve(); });
        expect(fetched).not.toHaveBeenCalled();
        view.source('owner-2');
        fireEvent.submit(dialog.querySelector('form')!);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        expect(JSON.parse(fetched.mock.calls[0][1].body)).toEqual({ name: 'Fjordline', ownerId: 'owner-2' });
    });

    it('captures same-turn DOM submits before the native bubbling handler', async () => {
        let complete!: (response: Response) => void;
        const fetched = vi.fn(() => new Promise<Response>(resolve => { complete = resolve; }));
        vi.stubGlobal('fetch', fetched);
        show();
        const dialog = await open();
        const form = dialog.querySelector('form')!;
        const bubbled = vi.fn();
        form.addEventListener('submit', bubbled);
        fireEvent.submit(form);
        fireEvent.submit(form);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        expect(bubbled).not.toHaveBeenCalled();
        await act(async () => { complete(response()); });
    });

    it.each([
        { isSuccess: false, isValid: false, validationResults: [{ members: ['name'], message: 'Server validation denied' }] },
        { isSuccess: false, hasExceptions: true, exceptionMessages: ['Server exception'] },
        { isSuccess: false, isAuthorized: false, authorizationFailureReason: 'Server authorization denied' },
    ])('releases the captured latch after a denied server outcome so correction can retry: %j', async outcome => {
        const fetched = vi.fn().mockResolvedValueOnce({ ok: true, json: async () => commandResult(outcome) }).mockResolvedValueOnce(response());
        vi.stubGlobal('fetch', fetched);
        show();
        const dialog = await open();
        fireEvent.submit(dialog.querySelector('form')!);
        await within(dialog).findAllByText(outcome.validationResults?.[0]?.message ?? outcome.exceptionMessages?.[0] ?? outcome.authorizationFailureReason!);
        expect(fetched).toHaveBeenCalledTimes(1);
        fireEvent.change(within(dialog).getByLabelText('Name', { exact: false }), { target: { value: 'Corrected' } });
        fireEvent.submit(dialog.querySelector('form')!);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(2));
        await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
    });

    it('ignores a retired session failure after cancel and reopen', async () => {
        let complete!: (response: Response) => void;
        const fetched = vi.fn(() => new Promise<Response>(resolve => { complete = resolve; }));
        vi.stubGlobal('fetch', fetched);
        show();
        const first = await open();
        fireEvent.submit(first.querySelector('form')!);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        fireEvent.keyDown(first, { key: 'Escape', code: 'Escape' });
        await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
        const reopened = await open();
        await act(async () => { complete({ ok: true, json: async () => commandResult({ isSuccess: false, hasExceptions: true, exceptionMessages: ['Retired session error'] }) } as Response); });
        expect(within(reopened).queryByRole('alert')).toBeNull();
        expect(screen.getByRole('dialog')).toBe(reopened);
    });

    it('does not let an old success close the reopened dialog or release its pending latch', async () => {
        const completions: ((response: Response) => void)[] = [];
        const fetched = vi.fn(() => new Promise<Response>(resolve => { completions.push(resolve); }));
        vi.stubGlobal('fetch', fetched);
        show();
        const first = await open();
        fireEvent.submit(first.querySelector('form')!);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(1));
        fireEvent.keyDown(first, { key: 'Escape', code: 'Escape' });
        await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
        const reopened = await open();
        fireEvent.submit(reopened.querySelector('form')!);
        await waitFor(() => expect(fetched).toHaveBeenCalledTimes(2));
        await act(async () => { completions[0](response()); });
        expect(screen.getByRole('dialog')).toBe(reopened);
        fireEvent.submit(reopened.querySelector('form')!);
        await act(async () => { await Promise.resolve(); });
        expect(fetched).toHaveBeenCalledTimes(2);
        await act(async () => { completions[1](response()); });
        await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
    });
});
