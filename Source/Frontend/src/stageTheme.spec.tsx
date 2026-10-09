// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, waitFor } from '@testing-library/react';
import { PrimeReactProvider } from '@primereact/core';
import { Button } from 'primereact/button';
import { describe, expect, it } from 'vitest';
import { SceneThemeProvider } from '@cratis/scene.react';
import { ColorSchemeMirror } from './StageChrome';
import { stageDarkSelector, stageTheme } from './stageTheme';

describe('the Stage theme', () => {
    it('makes PrimeReact inject the design tokens its components are painted with', () => {
        render(
            <PrimeReactProvider theme={stageTheme}>
                <Button>Execute</Button>
            </PrimeReactProvider>,
        );

        const injected = [...document.head.querySelectorAll('style')].map(style => style.textContent ?? '').join('\n');
        expect(injected).toContain('--p-primary-color');
    });

    it('keys its dark scheme off the colour scheme the blueprint theme sets on the document root', () => {
        expect(stageTheme.options.darkModeSelector).toBe(stageDarkSelector);
    });

    it('mirrors live blueprint theme changes onto the document root for portalled components', async () => {
        const { rerender } = render(
            <SceneThemeProvider theme={{ name: 'Light', compatibleWith: [], isDark: false }}>
                <ColorSchemeMirror />
            </SceneThemeProvider>,
        );

        await waitFor(() => expect(document.documentElement.getAttribute('data-scene-color-scheme')).toEqual('light'));
        rerender(
            <SceneThemeProvider theme={{ name: 'Dark', compatibleWith: [], isDark: true }}>
                <ColorSchemeMirror />
            </SceneThemeProvider>,
        );

        await waitFor(() => expect(document.documentElement.getAttribute('data-scene-color-scheme')).toEqual('dark'));
    });

    it('paints the page body with the active theme so switching to dark changes the applied background', async () => {
        const { rerender } = render(
            <SceneThemeProvider theme={{ name: 'Scene Default Light', compatibleWith: [], isDark: false, tokens: { 'surface.background': '#f6f7f9', 'text.color': '#1f2430' } }}>
                <ColorSchemeMirror />
            </SceneThemeProvider>,
        );
        await waitFor(() => expect(getComputedStyle(document.body).backgroundColor).toEqual('rgb(246, 247, 249)'));
        const light = getComputedStyle(document.body).backgroundColor;

        rerender(
            <SceneThemeProvider theme={{ name: 'Scene Default Dark', compatibleWith: [], isDark: true, tokens: { 'surface.background': '#0f1117', 'text.color': '#e6e8ef' } }}>
                <ColorSchemeMirror />
            </SceneThemeProvider>,
        );

        await waitFor(() => expect(getComputedStyle(document.body).backgroundColor).toEqual('rgb(15, 17, 23)'));
        expect(getComputedStyle(document.body).backgroundColor).not.toEqual(light);
        expect(getComputedStyle(document.body).color).toEqual('rgb(230, 232, 239)');
    });
});
