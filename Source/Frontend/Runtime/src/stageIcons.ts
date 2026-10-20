// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { componentIconLibrary } from '@cratis/scene.model';
import { createIconAdapterRegistry, type IconAdapter } from '@cratis/scene.react';
import { dynamicIconImports } from 'lucide-react/dynamic';

const aliases = new Map<string, keyof typeof dynamicIconImports>([
    ['chart', 'chart-column'],
    ['add', 'plus'],
    ['dollar', 'dollar-sign'],
    ['sitemap', 'network'],
    ['refresh', 'refresh-cw'],
    ['invoice', 'receipt-text'],
    ['batch', 'layers'],
]);

const normalizeKey = (key: string) => key.trim()
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/[\s_]+/g, '-')
    .toLowerCase();

export const stageLucideAdapter: IconAdapter = {
    library: componentIconLibrary,
    loadGlyph: async ({ key }) => {
        const normalized = normalizeKey(key);
        const resolved = aliases.get(normalized) ?? normalized;
        if (!Object.hasOwn(dynamicIconImports, resolved)) return undefined;
        return (await dynamicIconImports[resolved as keyof typeof dynamicIconImports]())?.default;
    },
};

export const stageIconAdapters = createIconAdapterRegistry([stageLucideAdapter]);
