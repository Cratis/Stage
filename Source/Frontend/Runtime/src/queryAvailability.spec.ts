// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { queryFailure, queryNotAvailable, querySuspended } from './stageData';

describe('what a query result says about why it has no rows', () => {
    it('says the read model is not available when it is not in the caller\'s offer', () => {
        expect(queryFailure({ isAuthorized: false, authorizationFailureReason: 'NotAvailable' })).toBe(queryNotAvailable);
    });

    it('says the read model is suspended when an operator suspended it, not that it is not available', () => {
        expect(queryFailure({ isAuthorized: false, authorizationFailureReason: 'Suspended' })).toBe(querySuspended);
    });

    it('keeps a caller who may not read it apart from both', () => {
        const failure = queryFailure({ isAuthorized: false, authorizationFailureReason: 'Needs the Manager role' });
        expect(failure).toBe('The query answered 403.');
        expect(failure).not.toBe(queryNotAvailable);
        expect(failure).not.toBe(querySuspended);
    });

    it('says nothing when the query was answered', () => {
        expect(queryFailure({ isAuthorized: true, isSuccess: true })).toBe('');
    });
});
