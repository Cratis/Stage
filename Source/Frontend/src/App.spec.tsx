// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, screen } from '@testing-library/react';
import { ExternalComponent, HorizontalAlignment, VerticalAlignment, Visibility } from '@cratis/scene.model';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { App, StageSceneApplication } from './App';

const scene: StageSceneApplication = {
    layouts: [],
    screenTemplates: [],
    screens: [{
        name: 'Invoices',
        layout: 'Application',
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
        vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => scene }));
    });

    it('renders a Screenplay screen through Scene React', async () => {
        render(<App />);

        expect(await screen.findByRole('heading', { name: 'Invoices' })).toBeDefined();
        expect(fetch).toHaveBeenCalledWith('stage/scene', expect.objectContaining({ signal: expect.any(AbortSignal) }));
    });
});
