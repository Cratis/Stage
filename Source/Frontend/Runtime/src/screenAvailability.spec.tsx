// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, act } from '@testing-library/react';
import { useEffect, useState } from 'react';
import { cleanup, fireEvent, render, screen, waitFor } from './testing/render';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { ComponentName, externalComponent } from '@cratis/scene.blueprint.default';
import { App, type AppProps } from './App';
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

function renderStage(availability: Promise<Response> | (() => Promise<Response>), address = '#/Home', options: {
    model?: unknown;
    routes?: () => Promise<Response>;
    components?: AppProps['components'];
} = {}) {
    globalThis.history.replaceState(null, '', address);
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url === 'stage/scene') return respond(options.model ?? scene);
        if (url === 'stage/routes') return options.routes?.() ?? respond(routes);
        if (url.startsWith('/.cratis/availability')) return typeof availability === 'function' ? availability() : availability;
        return respond([]);
    }));
    return render(<PrimeReactProvider theme={stageTheme}><App components={options.components} /></PrimeReactProvider>);
}

const navigationContribution = (label: string, targetScreen: string, destination?: Record<string, string>) => ({
    contributionPointName: 'Navigation',
    content: { id: label, name: label, componentName: 'core:contribution', properties: { label, targetScreen, hierarchy: ['Work'], ...(destination ? { destination } : {}) }, slots: {} },
});

function contributedScene(regions: boolean) {
    const home = {
        ...screenOf('Home'),
        contributions: [
            navigationContribution('Home entry', 'Home'),
            navigationContribution('Inherited restricted', 'Restricted'),
            navigationContribution('Restricted dialog entry', 'Home', { kind: 'dialog', dialog: 'Restricted' }),
        ],
        slotContent: { content: [...screenOf('Home').slotContent.content, {
            id: 'actions', name: 'actions', componentName: 'core:outletItems', slots: {},
            properties: { name: 'actions', kind: 'actions', items: [{ kind: 'button', id: 'open', title: 'Open restricted', navigateTo: { screen: 'Restricted' } }] },
        }] },
    };
    const restricted = { ...screenOf('Restricted'), contributions: [navigationContribution('Denied owner entry', 'Home')] };
    return { ...scene, navigation: regions ? scene.navigation : undefined, screens: [home, restricted] };
}

describe('screen availability', () => {
    afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals(); globalThis.history.replaceState(null, '', '#'); });

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

    it('revokes the hook decision on a failed refresh instead of retaining stale permission', async () => {
        const fetched = vi.fn().mockImplementationOnce(() => respond(answer(true))).mockRejectedValue(new Error('Offline'));
        vi.stubGlobal('fetch', fetched);
        const { result } = renderHook(() => useScreenAvailability(routes));
        await waitFor(() => expect(result.current('Restricted')).toBe(true));
        act(() => globalThis.dispatchEvent(new Event('focus')));
        await waitFor(() => expect(result.current('Restricted')).toBe(false));
    });

    it.each([
        [false, 'pending'], [false, 'unavailable'], [false, 'failed'],
        [true, 'pending'], [true, 'unavailable'], [true, 'failed'],
    ] as const)('filters inherited destinations and owners (regions=%s, %s)', async (regions, status) => {
        const availability = () => {
            if (status === 'pending') return new Promise<Response>(() => {});
            if (status === 'failed') return Promise.reject(new Error('Offline'));
            return respond(answer(false));
        };
        renderStage(availability, '#/Home', { model: contributedScene(regions) });
        await screen.findByRole('heading', { name: 'Home content' });
        for (const label of ['Inherited restricted', 'Restricted dialog entry', 'Denied owner entry', 'Open restricted']) {
            expect(screen.queryByText(label)).toBeNull();
        }
        expect(screen.queryByRole('link', { name: 'Restricted' })).toBeNull();
    });

    it.each([
        [false, 'pending'], [false, 'denied'], [false, 'failed'],
        [true, 'pending'], [true, 'denied'], [true, 'failed'],
    ] as const)('hides an authored breadcrumb Home destination (regions=%s, %s)', async (regions, status) => {
        const availability = () => {
            if (status === 'pending') return new Promise<Response>(() => {});
            if (status === 'failed') return Promise.reject(new Error('Offline'));
            return respond(answer(false));
        };
        const breadcrumb = externalComponent('authored-private-breadcrumb', ComponentName.Breadcrumb, {
            homeTargetScreen: 'Restricted', items: [{ label: 'Authored private trail' }],
        });
        const home = { ...screenOf('Home'), slotContent: { content: [...screenOf('Home').slotContent.content, breadcrumb] } };
        renderStage(availability, '#/Home', { model: { ...scene, navigation: regions ? scene.navigation : undefined, screens: [home, screenOf('Restricted')] } });
        await screen.findByRole('heading', { name: 'Home content' });
        expect(screen.queryByText('Authored private trail')).toBeNull();
        expect(document.querySelector('[data-scene-id="authored-private-breadcrumb"]')).toBeNull();
        expect(screen.queryAllByRole('link', { name: 'Home' }).some(link => link.getAttribute('href')?.includes('Restricted'))).toBe(false);
    });

    it('retains an authored breadcrumb Home link when its destination is available', async () => {
        const breadcrumb = externalComponent('authored-private-breadcrumb', ComponentName.Breadcrumb, {
            homeTargetScreen: 'Restricted', items: [{ label: 'Authored private trail' }],
        });
        const home = { ...screenOf('Home'), slotContent: { content: [...screenOf('Home').slotContent.content, breadcrumb] } };
        renderStage(respond(answer(true)), '#/Home', { model: { ...scene, screens: [home, screenOf('Restricted')] } });
        await screen.findByText('Authored private trail');
        expect(screen.getAllByRole('link', { name: 'Home' }).some(link => link.getAttribute('href')?.includes('Restricted'))).toBe(true);
    });

    it('keeps inherited contributions and opening actions when their owners and destinations are available', async () => {
        renderStage(respond(answer(true)), '#/Home', { model: contributedScene(false) });
        await screen.findByText('Inherited restricted');
        expect(screen.getByText('Restricted dialog entry')).toBeDefined();
        expect(screen.getByText('Denied owner entry')).toBeDefined();
        expect(screen.getByRole('button', { name: 'Open restricted' })).toBeDefined();
    });

    it('unmounts an available App screen and hides its entries after a failed refresh', async () => {
        const unmounted = vi.fn();
        function ProtectedContent() {
            useEffect(() => () => unmounted(), []);
            return <span>Mounted private content</span>;
        }
        const availability = vi.fn().mockImplementationOnce(() => respond(answer(true))).mockRejectedValue(new Error('Offline'));
        const restricted = { ...screenOf('Restricted'), slotContent: { content: [{ id: 'private', name: 'private', componentName: 'test:private', properties: {}, slots: {} }] } };
        renderStage(availability, '#/Restricted', { model: { ...scene, screens: [screenOf('Home'), restricted] }, components: { 'test:private': ProtectedContent } });
        const content = await screen.findByText('Mounted private content');
        act(() => globalThis.dispatchEvent(new Event('focus')));
        await screen.findByRole('alert');
        expect(content.isConnected).toBe(false);
        expect(unmounted).toHaveBeenCalledTimes(1);
        expect(screen.queryByText('Mounted private content')).toBeNull();
        expect(screen.queryByRole('link', { name: 'Restricted' })).toBeNull();
    });

    it.each(['pending', 'unavailable'])('hides %s entries in the fallback menu without navigation regions', async status => {
        renderStage(status === 'pending' ? new Promise(() => {}) : respond(answer(false)), '#/Home', { model: { ...scene, navigation: undefined } });
        await screen.findByRole('heading', { name: 'Home content' });
        expect(screen.queryByText('Restricted content')).toBeNull();
        expect(screen.queryByText('Restricted')).toBeNull();
    });

    it('keeps available fallback navigation entries without navigation regions', async () => {
        renderStage(respond(answer(true)), '#/Home', { model: { ...scene, navigation: undefined } });
        await screen.findByText('Restricted content');
        expect(screen.getByRole('heading', { name: 'Home content' })).toBeDefined();
    });

    it.each([
        () => Promise.reject(new Error('Routes unavailable')),
        () => Promise.resolve({ ok: false } as Response),
    ])('refuses routes and hides entries when the routes lookup fails', async routeResponse => {
        renderStage(respond(answer(true)), '#/Restricted', { routes: routeResponse });
        await screen.findByRole('alert');
        expect(screen.queryByText('Restricted content')).toBeNull();
        expect(screen.queryByRole('link', { name: 'Restricted' })).toBeNull();
        expect(screen.queryByRole('link', { name: 'Home' })).toBeNull();
    });

    it('accepts a successful legacy route response without screen gates', async () => {
        renderStage(new Promise(() => {}), '#/Restricted', { routes: () => respond({ commands: {} }) });
        await screen.findByText('Restricted content');
        expect(screen.getAllByRole('link', { name: 'Restricted' }).length).toBeGreaterThan(0);
    });

    it('preserves mounted form state during an unchanged periodic decision', async () => {
        vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
        let completeRefresh!: (response: Response) => void;
        const refresh = new Promise<Response>(resolve => { completeRefresh = resolve; });
        const availability = vi.fn().mockImplementationOnce(() => respond(answer(true))).mockReturnValue(refresh);
        function StatefulForm() {
            const [value, setValue] = useState('');
            return <input aria-label='Draft' value={value} onChange={event => setValue(event.target.value)} />;
        }
        const restricted = { ...screenOf('Restricted'), slotContent: { content: [{ id: 'draft', name: 'draft', componentName: 'test:stateful', properties: {}, slots: {} }] } };
        renderStage(availability, '#/Restricted', { model: { ...scene, screens: [screenOf('Home'), restricted] }, components: { 'test:stateful': StatefulForm } });
        const input = await screen.findByRole('textbox', { name: 'Draft' });
        fireEvent.change(input, { target: { value: 'Unsaved draft' } });
        act(() => vi.advanceTimersByTime(30_000));
        expect(availability).toHaveBeenCalledTimes(2);
        expect(screen.getByRole('textbox', { name: 'Draft' })).toBe(input);
        await act(async () => { completeRefresh(await respond(answer(true))); });
        expect(screen.getByRole('textbox', { name: 'Draft' })).toBe(input);
        expect((input as HTMLInputElement).value).toBe('Unsaved draft');
    });

    it.each(['pending', 'unavailable'])('hides a %s dialog opening action before mounting hostile content', async status => {
        const hostile = vi.fn(() => <span>Private dialog content</span>);
        const toolbar = {
            id: 'actions', name: 'actions', componentName: 'core:outletItems', slots: {},
            properties: { name: 'actions', kind: 'actions', items: [{ kind: 'button', id: 'open', title: 'Open restricted', navigateTo: { kind: 'dialog', dialog: 'Restricted', routeParameterBindings: {} } }] },
        };
        const model = {
            ...scene,
            screens: [{ ...screenOf('Home'), slotContent: { content: [...screenOf('Home').slotContent.content, toolbar] } }, screenOf('Restricted')],
            dialogTemplates: [{ name: 'Restricted', displayName: 'Private dialog', slots: [], content: { content: [{ id: 'private', name: 'private', componentName: 'test:hostile', properties: {}, slots: {} }] } }],
        };
        renderStage(status === 'pending' ? new Promise(() => {}) : respond(answer(false)), '#/Home', { model, components: { 'test:hostile': hostile } });
        await screen.findByRole('heading', { name: 'Home content' });
        expect(screen.queryByRole('button', { name: 'Open restricted' })).toBeNull();
        expect(screen.queryByRole('dialog', { name: 'Private dialog' })).toBeNull();
        expect(hostile).not.toHaveBeenCalled();
    });

    it('refuses a denied nested route before mounting hostile descendants', async () => {
        const hostile = vi.fn(() => <span>Protected descendant</span>);
        const restricted = { ...screenOf('Restricted'), slotContent: { content: [{ id: 'private', name: 'private', componentName: 'test:hostile', properties: {}, slots: {} }] } };
        renderStage(respond(answer(false)), '#/Home/@side/restricted', { model: { ...scene, screens: [screenOf('Home'), restricted] }, components: { 'test:hostile': hostile } });
        await screen.findByRole('alert');
        expect(hostile).not.toHaveBeenCalled();
    });

    it('does not mount guarded child components while pending', () => {
        const child = vi.fn(() => <span>Protected</span>);
        const Child = child;
        render(<ScreenAvailabilityGuard screens={['Restricted']} isAvailable={() => false}><Child /></ScreenAvailabilityGuard>);
        expect(child).not.toHaveBeenCalled();
    });
});
