// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Screen } from '@cratis/scene.model';

export const contribution = (label: string, targetScreen: string, hierarchy: string[], properties: Record<string, unknown> = {}) => ({
    contributionPointName: 'Navigation',
    content: { id: `${targetScreen}-nav`, name: targetScreen, componentName: 'core:contribution', properties: { label, targetScreen, hierarchy, ...properties }, slots: {} },
});

export const screen = (name: string, contributions: ReturnType<typeof contribution>[]): Screen => ({
    name, layout: 'AppShell', screenTemplate: undefined, forms: [], contributions, slotContent: {},
} as unknown as Screen);
