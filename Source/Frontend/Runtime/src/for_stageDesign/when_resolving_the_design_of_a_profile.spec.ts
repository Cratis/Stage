// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import type { DesignSystem, UiProfile } from '@cratis/scene.model';
import { ColorScheme } from '@cratis/scene.blueprint.default';
import { stageDesignOf } from '../stageDesign';

const brand: DesignSystem = {
    name: 'Brand',
    modes: ['light', 'dark'],
    tokens: [],
    components: [],
    appearance: { accent: 'color.primary' },
    themes: [
        {
            name: 'Brand',
            isDefault: true,
            modes: [
                { mode: 'light', tokens: { 'color.primary': '#0a7d33', 'color.background': '#ffffff', 'color.text': '#111111' } },
                { mode: 'dark', tokens: { 'color.primary': '#34d27b', 'color.background': '#0b1410', 'color.text': '#eef5f0' } },
            ],
        },
    ],
} as unknown as DesignSystem;

const profile = (extra: Partial<UiProfile> = {}) => ({ name: 'Web', targetPlatform: 'web', packages: [], ...extra }) as UiProfile;

describe('when the profile carries the design system the model declares', () => {
    it('should draw from that design system rather than the default one', () => {
        expect(stageDesignOf(profile({ designSystem: brand }), ColorScheme.Light, undefined).designSystem).toBe('Brand');
    });

    it('should give the tokens of the theme in the light mode', () => {
        expect(stageDesignOf(profile({ designSystem: brand }), ColorScheme.Light, undefined).tokens['color.primary']).toBe('#0a7d33');
    });

    it('should give the tokens of the theme in the dark mode once the shell is dark', () => {
        const design = stageDesignOf(profile({ designSystem: brand }), ColorScheme.Dark, undefined);

        expect([design.mode, design.tokens['color.primary']]).toEqual(['dark', '#34d27b']);
    });
});

describe('when the profile carries no design system', () => {
    it('should draw from the default design system the blueprint ships', () => {
        expect(stageDesignOf(profile(), ColorScheme.Light, undefined).designSystem).toBe('Scene Default');
    });
});

describe('when the profile has an accent', () => {
    it('should replace the primary colour of the theme with it', () => {
        expect(stageDesignOf(profile({ designSystem: brand }), ColorScheme.Light, '#e11d48').tokens['color.primary']).toBe('#e11d48');
    });

    it('should keep the primary colour of the theme without it', () => {
        expect(stageDesignOf(profile({ designSystem: brand, appearance: { accent: 'not-a-colour' } }), ColorScheme.Light, undefined).tokens['color.primary']).toBe('#0a7d33');
    });
});
