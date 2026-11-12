// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { vi } from 'vitest';
import type { ExternalComponent } from '@cratis/scene.model';
import { StageDataProvider } from '../../stageData';
import { StageExtensionComponent, StageExtensionServicesProvider, type StageExtensionServices } from '../../StageExtensionComponent';
import { forgetExtensionModules } from '../../stageExtensionModule';

export interface Call {
    url: string;
    method: string;
    body?: string;
}

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

/** A fetcher that answers the identity, the public settings and invocations, and remembers what it was asked. */
export function aHost(invoke: (call: Call) => Response = () => json({ status: 'Settled', outcome: 'done', output: '{"total":3}' })) {
    const calls: Call[] = [];
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const call: Call = { url: String(input), method: init?.method ?? 'GET', body: init?.body as string | undefined };
        calls.push(call);
        if (call.url === '.cratis/me') return json({ id: 'u-1', name: 'Ada', roles: ['Editor'] });
        if (call.url.endsWith('/settings')) return json({ settings: { region: 'eu' } });
        if (call.url.endsWith('/invoke')) return invoke(call);
        return json({}, 404);
    });
    return { calls, fetcher: fetcher as unknown as typeof fetch };
}

export const placement = (properties: Record<string, unknown> = {}): ExternalComponent => ({
    id: 'ledger-chart',
    componentName: 'Stage:extensionComponent',
    properties: {
        screen: 'Dashboard',
        system: 'ledger',
        component: 'Chart',
        kind: 'react',
        module: 'extension-assets/abc/index.js',
        invokes: ['Totals'],
        signals: ['pointChosen'],
        strings: { title: 'Totals' },
        properties: { series: 'sales' },
        ...properties
    },
    slots: {}
} as unknown as ExternalComponent);

export function aPlacedComponent(element: ExternalComponent, services: StageExtensionServices, locale = 'nb') {
    forgetExtensionModules();
    return render(
        <StageDataProvider routes={undefined} locale={locale} locales={['en', 'nb']} screen='Dashboard'>
            <StageExtensionServicesProvider services={services}>
                <StageExtensionComponent element={element} />
            </StageExtensionServicesProvider>
        </StageDataProvider>);
}
