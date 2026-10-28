// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it } from 'vitest';
import { navigateToScreen, screenPathWith, useStageRouting } from '../stageNavigation';

afterEach(() => { useStageRouting('hash'); globalThis.history.replaceState(null, '', '/'); });

describe('when the routing is by path', () => {
    it('addresses a screen as a regular route with its parameters', () => expect(screenPathWith('TalkPage', { id: 'a b', empty: '' })).toBe('/TalkPage?id=a+b'));

    it('writes the path and tells the navigation host', () => {
        useStageRouting('path');
        let popped = 0;
        globalThis.addEventListener('popstate', () => popped++);
        navigateToScreen('TalkList', {});
        expect(globalThis.location.pathname).toBe('/TalkList');
        expect(globalThis.location.hash).toBe('');
        expect(popped).toBe(1);
    });

    it('does not push the address it is already at', () => {
        useStageRouting('path');
        navigateToScreen('TalkList', {});
        const length = globalThis.history.length;
        navigateToScreen('TalkList', {});
        expect(globalThis.history.length).toBe(length);
    });
});

describe('when the routing is by hash', () => {
    it('writes the fragment, as it always has', () => {
        navigateToScreen('TalkList', {});
        expect(globalThis.location.hash).toBe('#/TalkList');
    });
});
