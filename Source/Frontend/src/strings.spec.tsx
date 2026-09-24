// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen } from '@testing-library/react';
import { ExternalComponent, HorizontalAlignment, VerticalAlignment, Visibility } from '@cratis/scene.model';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { App, StageSceneApplication } from './App';

function title(text: string): ExternalComponent {
    return {
        id: 'invoices-title',
        name: 'invoices-title',
        componentName: 'core:title',
        properties: { text },
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
    } as ExternalComponent;
}

function sceneWith(text: string): StageSceneApplication {
    return {
        layouts: [],
        screenTemplates: [],
        screens: [{
            name: 'Invoices',
            layout: 'Application',
            forms: [],
            contributions: [],
            slotContent: { content: [title(text)] },
        }],
    };
}

describe('resolving a $strings. reference at runtime', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/scene') return { ok: true, json: async () => sceneWith('$strings.invoice.listTitle') };
            if (url === 'stage/locales') return { ok: true, json: async () => ['en'] };
            if (url === 'stage/strings/en') return { ok: true, json: async () => ({ 'invoice.listTitle': 'Invoices' }) };
            return { ok: false, json: async () => ({}) };
        }));
    });

    it('shows the resolved text once the dictionary arrives, not the literal token', async () => {
        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeDefined();
        expect(screen.queryByText('$strings.invoice.listTitle')).toBeNull();
    });

    it('offers a locale switcher once more than one locale is available', async () => {
        (globalThis.fetch as ReturnType<typeof vi.fn>).mockImplementation(async (input: RequestInfo | URL) => {
            const url = String(input);
            if (url === 'stage/scene') return { ok: true, json: async () => sceneWith('$strings.invoice.listTitle') };
            if (url === 'stage/locales') return { ok: true, json: async () => ['en', 'no'] };
            if (url === 'stage/strings/en') return { ok: true, json: async () => ({ 'invoice.listTitle': 'Invoices' }) };
            return { ok: false, json: async () => ({}) };
        });

        render(<App />);

        expect(await screen.findByRole('combobox', { name: 'Locale' })).toBeDefined();
    });
});
