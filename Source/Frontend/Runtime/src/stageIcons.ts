// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { componentIconLibrary } from '@cratis/scene.model';
import { createIconAdapterRegistry, type IconAdapter } from '@cratis/scene.react';
import { dynamicIconImports } from 'lucide-react/dynamic';

export const stageLucideAdapter: IconAdapter = {
    library: componentIconLibrary,
    loadGlyph: async ({ key }) => (await dynamicIconImports[key as keyof typeof dynamicIconImports]?.())?.default,
};

export const stageIconAdapters = createIconAdapterRegistry([stageLucideAdapter]);
