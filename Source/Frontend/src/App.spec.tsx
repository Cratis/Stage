// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen } from '@testing-library/react';
import { ExternalComponent, HorizontalAlignment, VerticalAlignment, Visibility } from '@cratis/scene.model';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PrimeReactProvider } from '@primereact/core/config';
import { stageTheme } from './stageTheme';
import { App, StageSceneApplication } from './App';
import { StageSourceProvider, staticStageSource } from './stageSource';

const scene: StageSceneApplication = {
    layouts: [],
    screenTemplates: [],
    screens: [{
        name: 'Invoices',
        layout: 'AppShell',
        forms: [],
        contributions: [],
        slotContent: {
            content: [{
                id: 'invoices-title',
                name: 'invoices-title',
                componentName: 'core:title',
                properties: { text: 'Invoices' },
                slots: {},
                visibility: Visibility.Visible,
                isEnabled: true,
                opacity: 1,
                size: {},
                zIndex: 0,
                minimumSize: {},
                maximumSize: {},
                margin: { left: 0, top: 0, right: 0, bottom: 0 },
                horizontalAlignment: HorizontalAlignment.Stretch,
                verticalAlignment: VerticalAlignment.Stretch,
                borderThickness: { left: 0, top: 0, right: 0, bottom: 0 },
                padding: { left: 0, top: 0, right: 0, bottom: 0 },
                tabIndex: 0,
            } as ExternalComponent],
        },
    }],
};

describe('the Stage frontend', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/routes') return Promise.resolve({ ok: true, json: async () => ({ commands: {}, queries: {} }) });
            return Promise.resolve({ ok: true, json: async () => scene });
        }));
    });

    it('renders a Screenplay screen through Scene React', async () => {
        render(<PrimeReactProvider theme={stageTheme}><App /></PrimeReactProvider>);

        expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeDefined();
        expect(fetch).toHaveBeenCalledWith('stage/scene', expect.objectContaining({ signal: expect.any(AbortSignal) }));
    });

    it('renders content a generated application built in without requesting the Stage endpoints', async () => {
        const source = staticStageSource({ scene, routes: { commands: {}, queries: {} }, strings: { en: { greeting: 'Hello' } } });

        render(<PrimeReactProvider theme={stageTheme}><StageSourceProvider source={source}><App /></StageSourceProvider></PrimeReactProvider>);

        expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeDefined();
        expect(fetch).not.toHaveBeenCalled();
    });

    it('answers locales and dictionaries from built-in strings', async () => {
        const source = staticStageSource({ scene, routes: { commands: {}, queries: {} }, strings: { en: { greeting: 'Hello' }, nb: {} } });
        const signal = new AbortController().signal;

        expect(await source.locales(signal)).toEqual(['en', 'nb']);
        expect(await source.strings('en', signal)).toEqual({ greeting: 'Hello' });
        expect(await source.strings('de', signal)).toEqual({});
    });
});
