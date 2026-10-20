// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { Children, isValidElement, type ReactNode } from 'react';
import { Arc } from '@cratis/arc.react';
import { afterEach, expect, it, vi } from 'vitest';
import { clearBindings } from '@cratis/scene.components';
import { registeredQuery } from '../../Runtime/src/queryExecutor';
import { httpStageSource } from '../httpStageSource';

const render = vi.hoisted(() => vi.fn());
vi.mock('react-dom/client', () => ({ createRoot: () => ({ render }) }));
vi.mock('@cratis/stage.runtime', async importOriginal => ({ ...await importOriginal<typeof import('@cratis/stage.runtime')>(), App: () => null }));
vi.mock('../sharedModules', () => ({ registerSharedModules: vi.fn() }));
vi.mock('../stagePackages', () => ({
    loadStagePackages: async () => ({ components: {}, catalog: [], screenTemplates: [], dialogTemplates: [] }),
    StagePackagesFailed: class extends Error {},
}));

afterEach(() => { vi.restoreAllMocks(); clearBindings(); document.getElementById('root')?.remove(); });
it('registers native query proxies and selects direct transport before rendering the live host', async () => {
    const root = document.createElement('div');
    root.id = 'root';
    document.body.appendChild(root);
    vi.spyOn(httpStageSource, 'routes').mockResolvedValue({ commands: {}, queries: { Projects: '/projects' } });
    render.mockImplementation(tree => {
        expect(registeredQuery('Projects')).toBeDefined();
        expect(usesDirectQueries(tree)).toBe(true);
    });
    await import('../main');
    await waitFor(() => expect(render).toHaveBeenCalledOnce());
});

function usesDirectQueries(tree: ReactNode): boolean {
    return Children.toArray(tree).some(child => {
        if (!isValidElement<{ children?: ReactNode; queryDirectMode?: boolean }>(child)) return false;
        return child.type === Arc ? child.props.queryDirectMode === true : usesDirectQueries(child.props.children);
    });
}
