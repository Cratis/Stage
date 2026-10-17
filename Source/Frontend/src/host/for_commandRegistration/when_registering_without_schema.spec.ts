// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it, vi } from 'vitest';
import { resolveCommand } from '@cratis/scene.components';
import { registerStageCommands } from '../commandRegistration';

const should = chai.should();
describe('when registering a live-host command without schema metadata', () => {
    it('should warn and skip only the missing command while keeping the other commands available', () => {
        const warning = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
        try {
            registerStageCommands({ commands: { Missing: '/api/missing', Ready: '/api/ready' }, queries: {}, commandSchemas: { Ready: JSON.stringify({ properties: {} }) } });
            should.equal(resolveCommand('Missing'), undefined);
            should.exist(resolveCommand('Ready'));
            warning.mock.calls.should.have.lengthOf(1);
        } finally { warning.mockRestore(); }
    });
});
