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

describe('when no library is listed', () => {
    it('does not load the provider, theme or license for an undeclared profile', async () => {
        expect(await loadProfileProvider(undefined)).toBeUndefined();
        expect(evaluated).not.toHaveBeenCalled();
    });

    it('does not load the provider for a profile without packages', async () => {
        expect(await loadProfileProvider([{ name: 'App', packages: [] } as unknown as UiProfile])).toBeUndefined();
        expect(evaluated).not.toHaveBeenCalled();
    });
});
