// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import type { UiProfile } from '@cratis/scene.model';
import { loadProfileProvider } from '../loadProfileProvider';

const evaluated = vi.hoisted(() => vi.fn());
vi.mock('../primeReactProvider', () => {
    evaluated();
    return { withPrimeReact: vi.fn() };
});

describe('when PrimeReact is listed', () => {
    it('loads the provider entry before rendering', async () => {
        const provider = await loadProfileProvider([{ name: 'App', packages: ['PrimeReact'] } as unknown as UiProfile]);
        expect(evaluated).toHaveBeenCalledOnce();
        expect(provider?.withPrimeReact).toBeTypeOf('function');
    });
});
