// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { executeRegisteredCommand } from '../../commandExecutor';
import { commandResult } from '../../testing/commands';
import { registerStageCommands } from '../commandRegistration';

chai.should();
describe('when executing synthesized proxies with modeled responses', () => {
    afterEach(() => vi.unstubAllGlobals());
    it.each([
        { kind: 'string', response: 'hello' },
        { kind: 'number', response: 42 },
        { kind: 'number', response: 0 },
        { kind: 'boolean', response: true },
        { kind: 'boolean', response: false },
        { kind: 'object', response: { projectId: 'project-1', name: 'Apollo' } },
    ])('should preserve a $kind response of $response', async ({ kind, response }) => {
        const name = `Response_${kind}`;
        registerStageCommands({ commands: { [name]: '/api/response' }, queries: {}, commandSchemas: { [name]: JSON.stringify({ properties: {} }) }, commandResponseKinds: { [name]: kind } });
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(commandResult({ response })))));
        const outcome = await executeRegisteredCommand(name, {}, { microservice: '' });
        outcome.isSuccess.should.equal(true);
        chai.expect(outcome.response).to.deep.equal(response);
    });
});
