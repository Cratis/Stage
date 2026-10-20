// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { chai, describe, it } from 'vitest';
import { screenArguments, screenHashWith, screenParameters } from '../stageNavigation';

chai.should();

describe('when carrying native query arguments into a screen address', () => {
    it('should preserve a UUID as text when navigating and reloading', () => {
        const id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
        const args = screenArguments({ workItemId: Guid.parse(id) });

        args.should.deep.equal({ workItemId: id });
        screenParameters(screenHashWith('WorkItemDetails', args)).should.deep.equal({ workItemId: id });
    });

    it('should keep scalar values and omit missing values and structured objects as before', () => {
        screenArguments({ text: 'one', number: 0, flag: false, empty: '', missing: undefined,
            absent: null, record: { value: 'one' }, list: ['one'] }).should.deep.equal({ text: 'one', number: '0', flag: 'false' });
        screenArguments(undefined).should.deep.equal({});
    });
});
