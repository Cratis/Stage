// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { executeStageCommand } from '../stageCommands';

describe('when carrying a command response', () => {
    afterEach(() => vi.unstubAllGlobals());

    it.each(['11111111-1111-1111-1111-111111111111', { projectId: '11111111-1111-1111-1111-111111111111', name: 'Apollo' }])('preserves the accepted scalar or record response', async response => {
        vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => ({ isSuccess: true, response }) })));
        const outcome = await executeStageCommand('RegisterProject', '/register', { name: 'Apollo' });
        expect(outcome.response).toEqual(response);
        expect(outcome.isSuccess).toBe(true);
    });

    it('does not expose a response when the command is rejected', async () => {
        vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => ({ isSuccess: false, response: 'uncommitted' }) })));
        const outcome = await executeStageCommand('RegisterProject', '/register', { name: '' });
        expect(outcome.response).toBeUndefined();
        expect(outcome.isSuccess).toBe(false);
    });
});
