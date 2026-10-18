// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { Command } from '@cratis/arc/commands';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand } from '../commandExecutor';
import { commandResult } from '../testing/commands';

chai.should();
describe('when carrying an accepted command response', () => {
    afterEach(() => vi.unstubAllGlobals());
    it.each(['11111111-1111-1111-1111-111111111111', { projectId: '11111111-1111-1111-1111-111111111111', name: 'Apollo' }])('should preserve a scalar or record response', async response => {
        registerCommand('RegisterProject', class extends Command {
            readonly route = 'register';
            readonly propertyDescriptors = [];
            get requestParameters(): string[] { return []; }
            constructor() { super(typeof response === 'string' ? String : Object, false); }
        });
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(commandResult({ response })))));
        const outcome = await executeRegisteredCommand('RegisterProject', {}, { microservice: '' });
        outcome.isSuccess.should.equal(true);
        JSON.stringify(outcome.response).should.equal(JSON.stringify(response));
    });
});
