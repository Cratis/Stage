// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand } from '../../commandExecutor';
import { commandResult } from '../../testing/commands';
import { RegisterProject } from '../given/RegisterProject';

const should = chai.should();
describe('when executing with server exceptions', () => {
    afterEach(() => vi.unstubAllGlobals());
    it('should preserve exception messages without exposing an uncommitted response', async () => {
        registerCommand('RegisterProject', RegisterProject);
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(commandResult({
            isSuccess: false, hasExceptions: true, exceptionMessages: ['Commit failed'], response: { name: 'uncommitted' },
        })))));
        const outcome = await executeRegisteredCommand('RegisterProject', { name: 'Apollo' }, { microservice: '' });
        outcome.isSuccess.should.equal(false);
        outcome.messages.should.deep.equal(['Commit failed']);
        should.equal(outcome.response, undefined);
    });
});
