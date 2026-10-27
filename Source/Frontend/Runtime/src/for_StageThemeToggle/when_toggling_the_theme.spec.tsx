// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { LayoutConfigProvider } from '@cratis/scene.blueprint.default';
import type { ExternalComponent } from '@cratis/scene.model';
import { StageThemeToggle } from '../StageChrome';

const element = { id: 'topbar-theme', properties: {} } as unknown as ExternalComponent;
const draw = (themeName: string) => render(<LayoutConfigProvider storage={null} initialConfig={{ themeName }}><StageThemeToggle element={element} slots={{}} /></LayoutConfigProvider>);

describe('in a light theme', () => {
    it('offers one button to go dark, and no headline', () => {
        draw('Scene Default Light');
        expect(screen.queryByRole('heading')).toBeNull();
        expect(screen.queryByText('Theme')).toBeNull();
        expect(screen.getAllByRole('button')).toHaveLength(1);
        expect(screen.getByRole('button', { name: 'Switch to a dark theme' })).toBeTruthy();
    });

    it('then offers to go light again once pressed', () => {
        draw('Scene Default Light');
        fireEvent.click(screen.getByRole('button'));
        expect(screen.getByRole('button', { name: 'Switch to a light theme' })).toBeTruthy();
    });
});

describe('in a dark theme', () => {
    it('offers to go light', () => {
        draw('Scene Default Dark');
        expect(screen.getByRole('button', { name: 'Switch to a light theme' })).toBeTruthy();
    });
});
