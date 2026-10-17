// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand } from '../../commandExecutor';
import { commandResult } from '../../testing/commands';
import { RegisterProject } from '../given/RegisterProject';

const should = chai.should();
describe('when executing without authorization', () => {
    afterEach(() => vi.unstubAllGlobals());
    it('should reject the outcome and expose the authorization reason rather than a response', async () => {
        registerCommand('RegisterProject', RegisterProject);
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(commandResult({
            isAuthorized: false, authorizationFailureReason: 'Policy denied', response: { name: 'secret' },
        })))));
        const outcome = await executeRegisteredCommand('RegisterProject', { name: 'Apollo' }, { microservice: '' });
        outcome.isSuccess.should.equal(false);
        outcome.messages.should.deep.equal(['Policy denied']);
        should.equal(outcome.response, undefined);
    });
});
