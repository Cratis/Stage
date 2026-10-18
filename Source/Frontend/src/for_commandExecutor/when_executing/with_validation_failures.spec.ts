// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand } from '../../commandExecutor';
import { commandResult } from '../../testing/commands';
import { RegisterProject } from '../given/RegisterProject';

const should = chai.should();
describe('when executing with validation failures', () => {
    afterEach(() => vi.unstubAllGlobals());
    it('should carry every affected member and withhold the rejected response', async () => {
        registerCommand('RegisterProject', RegisterProject);
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(commandResult({
            isSuccess: false, isValid: false, response: { name: 'uncommitted' },
            validationResults: [{ severity: 3, members: ['name', 'alias'], message: 'Not unique', state: null }],
        })))));
        const outcome = await executeRegisteredCommand('RegisterProject', { name: 'Apollo' }, { microservice: '' });
        outcome.isSuccess.should.equal(false);
        outcome.validationErrors.should.deep.equal([{ member: 'name', message: 'Not unique' }, { member: 'alias', message: 'Not unique' }]);
        outcome.messages.should.deep.equal(['Not unique']);
        should.equal(outcome.response, undefined);
    });
});
