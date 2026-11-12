// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { reactComponent, useInvoke, useLocale, useRaise, useSettings, useStrings, useTenant, useUser } from '@cratis/scene.extensions';
import { aHost, aPlacedComponent, placement } from './given/a_placed_component';

function Chart({ series }: { series: string }) {
    const user = useUser();
    const locale = useLocale();
    const settings = useSettings();
    const text = useStrings();
    const tenant = useTenant();
    const invoke = useInvoke();
    const raise = useRaise();
    return (
        <div>
            <span data-testid='summary'>{`${series}|${user.name}|${user.roles.join()}|${locale.language}|${String(settings.region)}|${text('title')}|${tenant.id}`}</span>
            <button type='button' onClick={() => void invoke('Totals', { year: 2026 }).then(values => { document.title = String(values.total); })}>Load</button>
            <button type='button' onClick={() => raise('pointChosen', { index: 2 })}>Choose</button>
        </div>);
}

const importer = async () => ({ Chart: reactComponent(Chart) });

describe('when placing a react extension component', () => {
    it('shows the placeholder, marked busy, until the component has loaded', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        const region = view.getByRole('region', { name: 'ledger.Chart' });
        expect(region.getAttribute('aria-busy')).toBe('true');
        expect(region.getAttribute('data-kind')).toBe('react');
        await view.findByTestId('summary');
        expect(view.queryByRole('region', { name: 'ledger.Chart' })).toBeNull();
    });

    it('gives the component the properties, user, locale, public settings, strings and tenant of the contract', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        expect((await view.findByTestId('summary')).textContent).toBe('sales|Ada|Editor|nb|eu|Totals|default');
    });

    it('invokes an allowed ability through the host with one interaction identity', async () => {
        const host = aHost();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer });
        fireEvent.click(await view.findByRole('button', { name: 'Load' }));
        await waitFor(() => expect(document.title).toBe('3'));
        const call = host.calls.find(candidate => candidate.url.endsWith('/invoke'))!;
        expect(call.url).toBe('stage/extension-components/Dashboard/ledger/Chart/invoke');
        expect(call.method).toBe('POST');
        const body = JSON.parse(call.body!) as { ability: string; interaction: string; input: string };
        expect(body.ability).toBe('Totals');
        expect(body.interaction.length).toBeGreaterThan(0);
        expect(JSON.parse(body.input)).toEqual({ year: 2026 });
    });

    it('hands a raised signal to the screen', async () => {
        const host = aHost();
        const onSignal = vi.fn();
        const view = aPlacedComponent(placement(), { fetcher: host.fetcher, importer, onSignal });
        fireEvent.click(await view.findByRole('button', { name: 'Choose' }));
        expect(onSignal).toHaveBeenCalledWith({ screen: 'Dashboard', system: 'ledger', component: 'Chart', signal: 'pointChosen', values: { index: 2 } });
    });
});
