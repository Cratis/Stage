// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, act } from '@testing-library/react';
import { cleanup, render, screen, waitFor } from './testing/render';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { App } from './App';
import { stageTheme } from './stageTheme';
import { ScreenAvailabilityGuard, useScreenAvailability } from './screenAvailability';

const parts = { Restricted: 'StageScreen_exact' };
const routes = { commands: {}, screens: parts };
// Arc 23.5 AvailableParts / UnavailablePart serialized with the web JSON defaults.
const answer = (available: boolean) => ({ isSuccess: true, data: { unavailable: available ? [] : [{ name: 'StageScreen_exact', module: '', feature: '', slice: '' }] } });
const respond = (payload: unknown) => Promise.resolve({ ok: true, json: async () => payload } as Response);
const screenOf = (name: string) => ({
    name, layout: 'AppShell', path: ['Work', name], forms: [], behaviors: [], contributions: [],
    slotContent: { content: [{ id: name, name, componentName: 'core:title', properties: { text: `${name} content` }, slots: {} }] },
});
const scene = {
    layouts: [{ name: 'AppShell', slots: [{ name: 'topbar' }, { name: 'navigation' }, { name: 'content' }, { name: 'side', routed: true }] }],
    screenTemplates: [], dialogTemplates: [], uiProfiles: [],
    structure: { modules: [{ id: 'Work', name: 'Work', features: ['Home', 'Restricted'].map(name => ({ id: name, name, features: [], slices: [] })) }] },
    navigation: {
        application: { outlets: [{ name: 'topbar', kind: 'navigation', defaults: [{ kind: 'navigation', id: 'work', title: 'Work', target: { module: 'Work' } }] }] },
        modules: [{ module: 'Work', refinements: [{ outlet: 'sidebar', mode: 'overrides', items: ['Home', 'Restricted'].map(name => ({ kind: 'navigation', id: name, title: name, target: { module: 'Work', feature: name } })) }] }],
    },
    screens: [screenOf('Home'), screenOf('Restricted')],
};

function renderStage(availability: Promise<Response>, address = '#/Home') {
    globalThis.history.replaceState(null, '', address);
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url === 'stage/scene') return respond(scene);
        if (url === 'stage/routes') return respond({ commands: {}, queries: {}, screens: parts });
        if (url.startsWith('/.cratis/availability')) return availability;
        return respond([]);
    }));
    return render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);
}

describe('screen availability', () => {
    afterEach(() => { cleanup(); vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '#'); });

    it('hides gated navigation while pending without hiding an ungated screen', async () => {
        renderStage(new Promise(() => {}));
        await screen.findByText('Home content');
        expect(screen.queryByRole('link', { name: 'Restricted' })).toBeNull();
        expect(screen.getAllByRole('link', { name: 'Home' }).length).toBeGreaterThan(0);
    });

    it('refuses a pending deep link without mounting its content', async () => {
        renderStage(new Promise(() => {}), '#/Restricted');
        await screen.findByRole('alert');
        expect(screen.queryByText('Restricted content')).toBeNull();
    });

    it('shows the entry and mounts an available deep-linked screen', async () => {
        renderStage(respond(answer(true)), '#/Restricted');
        await screen.findByText('Restricted content');
        expect(screen.getAllByRole('link', { name: 'Restricted' }).length).toBeGreaterThan(0);
    });

    it('hides unavailable navigation', async () => {
        renderStage(respond(answer(false)));
        await screen.findByText('Home content');
        expect(screen.queryByRole('link', { name: 'Restricted' })).toBeNull();
    });

    it.each(['#/Restricted', '#/Home/@side/restricted'])('refuses the unavailable route %s without mounting its content', async address => {
        renderStage(respond(answer(false)), address);
        await screen.findByRole('alert');
        expect(screen.queryByText('Restricted content')).toBeNull();
    });

    it.each([
        () => Promise.reject(new Error('Network unavailable')),
        () => Promise.resolve({ ok: false } as Response),
        () => respond({ isSuccess: true, data: { unavailable: [{ name: null }] } }),
        () => respond({ data: {} }),
    ])('fails closed on errors and malformed responses', async response => {
        vi.stubGlobal('fetch', vi.fn(response));
        const { result } = renderHook(() => useScreenAvailability(routes));
        expect(result.current('Restricted')).toBe(false);
        await act(async () => { await response().catch(() => {}); });
        expect(result.current('Restricted')).toBe(false);
        expect(result.current('Home')).toBe(true);
        expect(result.current('constructor')).toBe(true);
    });

    it('matches exact part names, not another part in the same module, feature or slice', async () => {
        vi.stubGlobal('fetch', vi.fn(() => respond({ isSuccess: true, data: { unavailable: [{ name: 'AnotherPart', module: 'Work', feature: 'Restricted', slice: 'Restricted' }] } })));
        const { result } = renderHook(() => useScreenAvailability(routes));
        await waitFor(() => expect(result.current('Restricted')).toBe(true));
    });

    it('does not fetch for an application with no gated screens', () => {
        const fetched = vi.fn();
        vi.stubGlobal('fetch', fetched);
        const { result } = renderHook(() => useScreenAvailability(undefined));
        expect(result.current('Home')).toBe(true);
        expect(fetched).not.toHaveBeenCalled();
    });

    it('unmounts denied content on a failed refresh instead of retaining stale permission', async () => {
        const fetched = vi.fn().mockImplementationOnce(() => respond(answer(true))).mockRejectedValue(new Error('Offline'));
        vi.stubGlobal('fetch', fetched);
        const { result } = renderHook(() => useScreenAvailability(routes));
        await waitFor(() => expect(result.current('Restricted')).toBe(true));
        act(() => globalThis.dispatchEvent(new Event('focus')));
        await waitFor(() => expect(result.current('Restricted')).toBe(false));
    });

    it('does not mount guarded child components while pending', () => {
        const child = vi.fn(() => <span>Protected</span>);
        const Child = child;
        render(<ScreenAvailabilityGuard screens={['Restricted']} isAvailable={() => false}><Child /></ScreenAvailabilityGuard>);
        expect(child).not.toHaveBeenCalled();
    });
});
