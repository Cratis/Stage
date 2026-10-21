// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { Children, isValidElement, type ReactNode } from 'react';
import { Arc } from '@cratis/arc.react';
import { afterEach, expect, it, vi } from 'vitest';
import { httpStageSource } from '../httpStageSource';

const render = vi.hoisted(() => vi.fn());
vi.mock('react-dom/client', () => ({ createRoot: () => ({ render }) }));
vi.mock('@cratis/stage.runtime', async importOriginal => ({ ...await importOriginal<typeof import('@cratis/stage.runtime')>(), App: () => null }));
vi.mock('../sharedModules', () => ({ registerSharedModules: vi.fn() }));
vi.mock('../stagePackages', () => ({
    loadStagePackages: async () => ({ components: {}, catalog: [], screenTemplates: [], dialogTemplates: [] }),
    StagePackagesFailed: class extends Error {},
}));

const prefix = '/api/play/17695861-60d0-4791-9e45-8b87c4c7aed4';

afterEach(() => {
    vi.restoreAllMocks();
    document.getElementById('root')?.remove();
    globalThis.history.replaceState(null, '', '/');
});

it("gives Arc the prefix the page is served under, so its own requests such as /.cratis/me reach the Stage", async () => {
    globalThis.history.replaceState(null, '', `${prefix}/#/PricingBoard`);
    const root = document.createElement('div');
    root.id = 'root';
    document.body.appendChild(root);
    vi.spyOn(httpStageSource, 'routes').mockResolvedValue({ commands: {}, queries: {} });

    await import('../main');

    await waitFor(() => expect(render).toHaveBeenCalledOnce());
    expect(arcApiBasePath(render.mock.calls[0][0])).toBe(prefix);
});

function arcApiBasePath(tree: ReactNode): string | undefined {
    for (const child of Children.toArray(tree)) {
        if (!isValidElement<{ children?: ReactNode; apiBasePath?: string }>(child)) continue;
        const found = child.type === Arc ? child.props.apiBasePath : arcApiBasePath(child.props.children);
        if (found !== undefined) return found;
    }
    return undefined;
}
