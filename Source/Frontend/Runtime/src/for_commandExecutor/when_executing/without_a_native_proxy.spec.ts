// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, chai, describe, it, vi } from 'vitest';
import { registerCommand } from '@cratis/scene.components';
import { executeRegisteredCommand } from '../../commandExecutor';

chai.should();
describe('when executing without a native Arc proxy', () => {
    afterEach(() => vi.unstubAllGlobals());
    it.each(['Missing', 'NotACommand'])('should refuse %s without sending a request', async name => {
        registerCommand('NotACommand', class {});
        const fetched = vi.fn();
        vi.stubGlobal('fetch', fetched);
        const outcome = await executeRegisteredCommand(name, {}, { microservice: '' });
        outcome.isSuccess.should.equal(false);
        fetched.mock.calls.should.have.lengthOf(0);
        outcome.messages.should.have.lengthOf(1);
    });
});
