// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, renderHook, waitFor } from '@testing-library/react';
import { createElement, type ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { StageSourceProvider, type StageSource } from '../stageSource';
import { useStrings } from '../useStrings';

const never = (signal: AbortSignal) => new Promise<never>((_, reject) => signal.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError'))));

describe('when the locale changes while the earlier one is still loading', () => {
    const source: StageSource = {
        scene: () => never(new AbortController().signal),
        routes: async () => undefined,
        locales: async () => ['en', 'nb'],
        strings: (locale, signal) => locale === 'en' ? never(signal) : Promise.resolve({ greeting: 'Hei' }),
    };
    const wrapper = ({ children }: { children: ReactNode }) => createElement(StageSourceProvider, { source }, children);

    it('keeps the dictionary of the locale now active, not an empty one from the cancelled request', async () => {
        const { result } = renderHook(() => useStrings(), { wrapper });
        await waitFor(() => expect(result.current.locale).toBe('en'));
        act(() => result.current.setLocale('nb'));
        await waitFor(() => expect(result.current.dictionary).toEqual({ greeting: 'Hei' }));
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(result.current.dictionary).toEqual({ greeting: 'Hei' });
    });
});
