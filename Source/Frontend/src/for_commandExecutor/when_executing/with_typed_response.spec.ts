// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand, registeredCommand } from '../../commandExecutor';
import { commandResult } from '../../testing/commands';
import { RegisterProject } from '../given/RegisterProject';
import { ProjectResponse } from '../given/ProjectResponse';

chai.should();
describe('when executing a command with a typed response', () => {
    afterEach(() => vi.unstubAllGlobals());
    it('should preserve the response instantiated by the proxy and assign only declared command properties', async () => {
        registerCommand('RegisterProject', RegisterProject);
        chai.should().equal(registeredCommand('RegisterProject', 'Projects.RegisterProject', '/api/projects/register'), RegisterProject);
        chai.should().equal(registeredCommand('RegisterProject', 'Other.RegisterProject', '/api/other/register'), undefined);
        const bodies: string[] = [];
        vi.stubGlobal('fetch', vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
            bodies.push(String(init?.body));
            return new Response(JSON.stringify(commandResult({ response: { name: 'Hermes' } })));
        }));
        const outcome = await executeRegisteredCommand('RegisterProject', { name: 'Hermes', route: '/wrong', unrelated: 'ignored' }, { microservice: '' });
        outcome.isSuccess.should.equal(true);
        (outcome.response instanceof ProjectResponse).should.equal(true);
        bodies.should.deep.equal([JSON.stringify({ name: 'Hermes' })]);
        outcome.messages.should.deep.equal([]);
    });
});
