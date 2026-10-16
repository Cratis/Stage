// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { ApplicationHost, type ApplicationDocument } from '@cratis/scene.blueprint.default';
import { App, stageShellOf, type StageSceneApplication } from './App';
import { stageProfile, stageRegistry } from './blueprint';
import { stageNavigationModel } from './stageNavigationModel';

const element = (id: string, componentName: string, properties: Record<string, unknown> = {}) => ({ id, name: id, componentName, properties, slots: {} });

const baseScene = (uiProfile: Record<string, unknown> | undefined, layouts: unknown[] = [], screenTemplates: unknown[] = []): StageSceneApplication => ({
    layouts,
    screenTemplates,
    uiProfiles: uiProfile ? [{ name: 'Web', targetPlatform: 'web', packages: ['core', 'PrimeReact', 'Cratis.Blueprint.Default'], ...uiProfile }] : [],
    screens: [{
        name: 'Invoices',
        layout: 'AppShell',
        forms: [],
        contributions: [],
        slotContent: { content: [element('title', 'core:title', { text: 'Invoices' })] },
    }],
} as unknown as StageSceneApplication);

function show(scene: StageSceneApplication) {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url === 'stage/routes') return Promise.resolve({ ok: true, json: () => Promise.resolve({ commands: {}, queries: {} }) } as Response);
        return Promise.resolve({ ok: true, json: () => Promise.resolve(scene) } as Response);
    }));
    return render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);
}

const blueprintRoot = (options?: Record<string, unknown>) => ({
    root: { template: { source: 'Blueprint', id: 'AppShell', blueprint: 'Cratis.Blueprint.Default', version: '^1.0' }, ...(options ? { options } : {}) },
});

describe('when the profile roots the application on a template', () => {
    afterEach(() => {
        cleanup();
        document.documentElement.removeAttribute('data-scene-color-scheme');
        globalThis.localStorage?.clear();
        vi.unstubAllGlobals();
    });

    it('should draw the blueprint shell the root names', async () => {
        show(baseScene(blueprintRoot()));

        expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeDefined();
        expect(document.querySelector('.layout-wrapper')).not.toBeNull();
    });

    it('should resolve the effective shell through the Scene engine', () => {
        const shell = stageShellOf(baseScene(blueprintRoot()));

        expect(shell.name).toBe('AppShell');
        expect(shell.origin).toBe('root');
        expect(shell.source).toBe('blueprint');
    });

    it('should draw the application\'s own shell when the root names one', async () => {
        const own = { name: 'AppShell', slots: ['topbar', 'sidebar', 'menu', 'breadcrumb', 'content', 'footer', 'rightPanel', 'configPanel'].map(name => ({ name })) };
        const scene = baseScene({ root: { template: { source: 'Own', id: 'AppShell' } } }, [own]);

        expect(stageShellOf(scene).source).toBe('own');
        show(scene);
        expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeDefined();
    });

    it('should start the sidebar in the mode the navigationMode option names', async () => {
        show(baseScene(blueprintRoot({ navigationMode: 'slim' })));

        await screen.findByRole('heading', { name: 'Invoices' });
        await waitFor(() => expect(document.querySelector('.layout-wrapper')?.className).toContain('layout-slim'));
    });

    it('should leave the footer out when showFooter is false and draw it otherwise', async () => {
        const { unmount } = show(baseScene(blueprintRoot({ showFooter: false })));
        await screen.findByRole('heading', { name: 'Invoices' });
        expect(document.querySelector('footer')).toBeNull();
        unmount();

        show(baseScene(blueprintRoot()));
        await screen.findByRole('heading', { name: 'Invoices' });
        expect(document.querySelector('footer')).not.toBeNull();
    });

    it('should apply a dark colour scheme and the accent as the theme\'s primary colour', async () => {
        show(baseScene({ ...blueprintRoot(), appearance: { colorScheme: 'Dark', accent: '#e11d48' } }));

        await screen.findByRole('heading', { name: 'Invoices' });
        await waitFor(() => expect(document.documentElement.getAttribute('data-scene-color-scheme')).toBe('dark'));
        const themed = document.querySelector('[style*="--scene-primary-color"]') as HTMLElement | null;
        expect(themed?.style.getPropertyValue('--scene-primary-color')).toBe('#e11d48');
    });

    it('should follow the system colour scheme when the profile says system', async () => {
        vi.stubGlobal('matchMedia', (query: string) => ({ matches: true, media: query, addEventListener: () => undefined, removeEventListener: () => undefined }));
        show(baseScene({ ...blueprintRoot(), appearance: { colorScheme: 'System' } }));

        await screen.findByRole('heading', { name: 'Invoices' });
        await waitFor(() => expect(document.documentElement.getAttribute('data-scene-color-scheme')).toBe('dark'));
    });
});

describe('when the root template cannot be the shell', () => {
    afterEach(() => {
        cleanup();
        vi.unstubAllGlobals();
    });

    it('should state an unknown own template at startup instead of a blank page or another shell', async () => {
        show(baseScene({ root: { template: { source: 'Own', id: 'Nowhere' } } }));

        const alert = await screen.findByRole('alert');
        expect(alert.getAttribute('data-stage-startup-problem')).toBe('root-template');
        expect(alert.textContent).toContain('Nowhere');
        expect(screen.queryByRole('heading', { name: 'Invoices' })).toBeNull();
    });

    it('should state a version pin the installed blueprint does not satisfy, with the installed version', async () => {
        const scene = baseScene({ root: { template: { source: 'Blueprint', id: 'AppShell', blueprint: 'Cratis.Blueprint.Default', version: '^9.0' } } });
        show(scene);

        const alert = await screen.findByRole('alert');
        expect(alert.textContent).toContain('^9.0');
        expect(alert.textContent).toContain('1.0.0');
    });

    it('should state a blueprint that is not installed', async () => {
        show(baseScene({ root: { template: { source: 'Blueprint', id: 'AppShell', blueprint: 'Cratis.Blueprint.Crm' } } }));

        expect((await screen.findByRole('alert')).textContent).toContain('Cratis.Blueprint.Crm');
    });
});

describe('when the same application runs in the Stage and in the Scene preview host', () => {
    afterEach(() => {
        cleanup();
        globalThis.localStorage?.clear();
        vi.unstubAllGlobals();
    });

    // The wrapper classes carry everything the shell decides: its mode, menu tint and scheme. They are the shell's own output,
    // so the same profile has to give the same wrapper whichever host draws it.
    const wrapperOf = () => (document.querySelector('.layout-wrapper') as HTMLElement).className.split(/\s+/).filter(Boolean).sort();

    it.each([
        ['a blueprint root with options', baseScene(blueprintRoot({ navigationMode: 'slim', menuTheme: 'dark' }))],
        ['a blueprint root', baseScene(blueprintRoot())],
    ])('should draw %s with the same shell', async (_, scene) => {
        const first = show(scene);
        await screen.findByRole('heading', { name: 'Invoices' });
        const stage = wrapperOf();
        first.unmount();
        globalThis.localStorage?.clear();

        const document_ = scene as unknown as ApplicationDocument;
        const preview = render(
            <PrimeReactProvider theme={stageTheme}>
                <ApplicationHost
                    application={document_}
                    composition={{ profile: stageProfile(scene.uiProfiles) }}
                    navigationModel={stageNavigationModel(scene)}
                    registry={stageRegistry({})}
                    initialScreen='Invoices'
                    layoutConfigStorage={null}
                />
            </PrimeReactProvider>);
        await screen.findByRole('heading', { name: 'Invoices' });
        const previewed = wrapperOf();
        preview.unmount();

        // The Stage applies the profile's options; the preview host starts the shell in its defaults, so the modes may differ only
        // by what the options set - everything else is identical.
        const modeOnly = (classes: string[]) => classes.filter(name => !/^layout-(static|overlay|slim|slim-plus|compact|horizontal|reveal|drawer|menu-(light|dark|primary))$/.test(name));
        expect(modeOnly(stage)).toEqual(modeOnly(previewed));
    });
});
