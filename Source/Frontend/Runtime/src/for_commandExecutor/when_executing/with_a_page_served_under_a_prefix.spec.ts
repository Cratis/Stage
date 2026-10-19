// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand, hostedBasePath } from '../../commandExecutor';
import { commandResult } from '../../testing/commands';
import { RegisterProject } from '../given/RegisterProject';

chai.should();
describe('when executing a command in a page served under a path prefix', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
        globalThis.history.replaceState(null, '', '/');
    });

    it('should post to the prefix the page is served under and not to the root of the host', async () => {
        registerCommand('RegisterProject', RegisterProject);
        globalThis.history.replaceState(null, '', '/api/play/0d0f6d8e-1f5c-4a9f-9b57-6a1f6a5b7c11/#/Suppliers');
        const urls: string[] = [];
        vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
            urls.push(String(input));
            return new Response(JSON.stringify(commandResult()));
        }));

        await executeRegisteredCommand('RegisterProject', { name: 'Hermes' }, { microservice: '' });

        hostedBasePath().should.equal('/api/play/0d0f6d8e-1f5c-4a9f-9b57-6a1f6a5b7c11');
        urls.should.have.lengthOf(1);
        new URL(urls[0], 'http://localhost').pathname.should.equal('/api/play/0d0f6d8e-1f5c-4a9f-9b57-6a1f6a5b7c11/api/projects/register');
    });

    it('should leave the root of a host alone', async () => {
        globalThis.history.replaceState(null, '', '/#/Suppliers');
        hostedBasePath().should.equal('');
    });
});
