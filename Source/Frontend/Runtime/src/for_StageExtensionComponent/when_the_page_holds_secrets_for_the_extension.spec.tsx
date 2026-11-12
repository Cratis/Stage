// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { reactComponent, useInvoke, useSettings } from '@cratis/scene.extensions';
import { ExtensionInvocationError } from '../stageExtensionContext';
import { aHost, aPlacedComponent, placement } from './given/a_placed_component';

let failure: unknown;

function Probe() {
    const invoke = useInvoke();
    const settings = useSettings();
    return (
        <div>
            <span data-testid='settings'>{JSON.stringify(settings)}</span>
            <button type='button' onClick={() => { invoke('Secrets').catch(error => { failure = error; }); }}>Try</button>
            <button type='button' onClick={() => { invoke('Totals').catch(error => { failure = error; }); }}>Totals</button>
        </div>);
}

const importer = async () => ({ Chart: reactComponent(Probe) });

describe('when the page holds secrets for the extension', () => {
    it('only holds what the settings endpoint answered, never a secret the host did not send', async () => {
        const view = aPlacedComponent(placement(), { fetcher: aHost().fetcher, importer });
        expect((await view.findByTestId('settings')).textContent).toBe('{"region":"eu"}');
    });

    it('asks the host for nothing but the identity, the public settings and invocations', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        await view.findByTestId('settings');
        expect(host.calls.map(call => call.url).sort()).toEqual(['.cratis/me', 'stage/extension-components/Dashboard/ledger/Chart/settings']);
    });

    it('refuses an ability outside the contract without calling the host', async () => {
        failure = undefined;
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        fireEvent.click(await view.findByRole('button', { name: 'Try' }));
        await waitFor(() => expect(failure).toBeInstanceOf(ExtensionInvocationError));
        expect(host.calls.some(call => call.url.endsWith('/invoke'))).toBe(false);
    });

    it('carries nothing of the settings or the person in an invocation', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        fireEvent.click(await view.findByRole('button', { name: 'Totals' }));
        await waitFor(() => expect(host.calls.some(call => call.url.endsWith('/invoke'))).toBe(true));
        const body = host.calls.find(call => call.url.endsWith('/invoke'))!.body!;
        expect(Object.keys(JSON.parse(body) as object).sort()).toEqual(['ability', 'input', 'interaction']);
        expect(body).not.toContain('eu');
        expect(body).not.toContain('Ada');
    });

    it('surfaces what the host refused with, so the component can say why', async () => {
        failure = undefined;
        const host = aHost(() => new Response(JSON.stringify({ status: 'Forbidden', reason: 'You are not authorized.' }), { status: 403 }));
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        fireEvent.click(await view.findByRole('button', { name: 'Totals' }));
        await waitFor(() => expect(failure).toBeInstanceOf(ExtensionInvocationError));
        expect((failure as ExtensionInvocationError).status).toBe(403);
        expect((failure as ExtensionInvocationError).message).toBe('You are not authorized.');
    });
});
