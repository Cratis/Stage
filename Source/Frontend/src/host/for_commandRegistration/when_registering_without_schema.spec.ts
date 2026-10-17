// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { registerStageCommands } from '../commandRegistration';

chai.should();
describe('when registering a live-host command without schema metadata', () => {
    it('should refuse registration instead of silently discarding its payload', () => {
        (() => registerStageCommands({ commands: { Save: '/api/save' }, queries: {} })).should.throw();
    });
});
