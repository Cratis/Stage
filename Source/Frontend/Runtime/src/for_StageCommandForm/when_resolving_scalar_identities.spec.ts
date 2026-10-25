// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { selectedIdentifier, selectedRowsOf, sessionIdentifiers } from '../StageCommandForm';
import type { SchemaProperty } from '../stageFormSchema';

chai.should();

const property: SchemaProperty = { name: 'organisationId', type: 'number', identifier: 'OrganisationKey', required: true };

describe('when resolving selected scalar identities', () => {
    it.each([42, 0, '42', '0'])('should preserve key %s in a command whose identity is not a reference', value => {
        sessionIdentifiers([], [property], {}, [{ organisationId: value }]).should.deep.equal({ organisationId: value });
    });

    it.each([undefined, null, '', Number.NaN, Number.POSITIVE_INFINITY, {}, []])('should treat an unusable key as absent (%s)', value => {
        (selectedIdentifier(property, [{ organisationId: value }]) === undefined).should.equal(true);
    });

    it('should not equate numeric and string keys when selected rows disagree', () => {
        (selectedIdentifier(property, [{ organisationId: 42 }, { organisationId: '42' }]) === undefined).should.equal(true);
    });

    it.each([42, 0])('should retain a numeric row id when exposing its modeled alias (%s)', value => {
        const row = { id: value };
        selectedRowsOf({ organisations: row }, { organisations: {
            name: 'Organisation', route: '/organisations', rows: [row], loading: false, error: '', arguments: {},
        } }).should.deep.equal([{ id: value, organisationId: value }]);
    });
});
