// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it } from 'vitest';
import { screenHref, useStageRouting } from '../stageNavigation';

chai.should();

afterEach(() => {
    useStageRouting('hash');
    globalThis.history.replaceState(null, '', '/');
});

describe('when addressing a screen link', () => {
    it.each([
        ['hash', 'A', '#/A'],
        ['path', 'A', '/A'],
        ['hash', 'A / B?', '#/A%20%2F%20B%3F'],
        ['path', 'A / B?', '/A%20%2F%20B%3F'],
    ] as const)('should address %s routing for %s', (routing, screen, address) => {
        useStageRouting(routing);

        screenHref(screen).should.equal(address);
    });
});
