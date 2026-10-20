// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, chai, describe, it } from 'vitest';
import { Command } from '@cratis/arc/commands';
import { DateOnly, JsonSerializer } from '@cratis/fundamentals';
import { resolveCommand } from '@cratis/scene.components';
import { registerStageCommands } from '../commandRegistration';

chai.should();
describe('when serializing live-host date fields', () => {
    let command: Command;

    beforeEach(() => {
        registerStageCommands({ commands: { SaveDates: '/api/save-dates' }, queries: {}, commandSchemas: { SaveDates: JSON.stringify({
            properties: { birthDate: { type: 'string', format: 'date' }, appointment: { type: 'string', format: 'date-time' } },
            required: ['birthDate'],
        }) } });
        const type = resolveCommand('SaveDates');
        if (!type) throw new Error('SaveDates was not registered');
        command = new type() as Command;
    });

    function serialized(name: string, value: unknown): string {
        Reflect.set(command, name, value);
        return JsonSerializer.serialize({ [name]: Reflect.get(command, name) });
    }

    it('should describe a date-only property as DateOnly and an instant as Date', () => {
        command.propertyDescriptors.map(descriptor => [descriptor.name, descriptor.type, descriptor.isOptional]).should.deep.equal([
            ['birthDate', DateOnly, false], ['appointment', Date, true],
        ]);
    });

    it.each([0, 23])('should serialize the picked local day at hour %i without shifting to UTC', hour => {
        serialized('birthDate', new Date(2020, 0, 1, hour)).should.equal('{"birthDate":"2020-01-01"}');
    });

    it('should coerce a calendar-date string to DateOnly', () => {
        serialized('birthDate', '2020-01-01').should.equal('{"birthDate":"2020-01-01"}');
        Reflect.get(command, 'birthDate').should.be.instanceOf(DateOnly);
    });

    it.each(['not-a-date', '2020-02-30', '2020-01-01T00:00:00.000Z'])('should leave invalid date-only text %s for the server to reject', value => {
        serialized('birthDate', value).should.equal(JSON.stringify({ birthDate: value }));
    });

    it('should treat a normalized initial calendar date as unchanged and restore it after an edit', () => {
        command.setInitialValues({ birthDate: '2020-01-01' });
        command.hasChanges.should.be.false;
        serialized('birthDate', new Date(2020, 0, 2));
        command.hasChanges.should.be.true;
        command.revertChanges();
        command.hasChanges.should.be.false;
        JsonSerializer.serialize({ birthDate: Reflect.get(command, 'birthDate') }).should.equal('{"birthDate":"2020-01-01"}');
    });

    it('should preserve an existing DateOnly value', () => {
        serialized('birthDate', DateOnly.parse('2020-01-01')).should.equal('{"birthDate":"2020-01-01"}');
    });

    it('should keep date-time serialization as an ISO instant', () => {
        const instant = new Date(2020, 0, 1, 23, 15);
        serialized('appointment', instant).should.equal(JSON.stringify({ appointment: instant.toISOString() }));
    });
});
