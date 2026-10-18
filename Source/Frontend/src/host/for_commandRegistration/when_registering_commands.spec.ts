// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { chai, describe, it } from 'vitest';
import { Command } from '@cratis/arc/commands';
import { resolveCommand } from '@cratis/scene.components';
import { registerStageCommands } from '../commandRegistration';

chai.should();
describe('when registering live-host commands', () => {
    it('should preserve all schema members, types and optionality before the runtime resolves them', () => {
        registerStageCommands({ commands: { Save: '/api/save' }, queries: {}, commandSchemas: { Save: JSON.stringify({
            properties: { title: { type: 'string' }, count: { type: ['integer', 'null'] }, enabled: { type: 'boolean' }, at: { type: 'string', format: 'date-time' }, tags: { type: 'array' }, details: { type: 'object' } },
            required: ['title', 'enabled', 'at', 'tags', 'details'],
        }) } });
        const type = resolveCommand('Save');
        if (!type) throw new Error('Save was not registered');
        const proxy = new type();
        if (!(proxy instanceof Command)) throw new Error('Save is not an Arc command');
        proxy.route.should.equal('api/save');
        proxy.propertyDescriptors.map(descriptor => [descriptor.name, descriptor.type, descriptor.isOptional]).should.deep.equal([
            ['title', String, false], ['count', Number, true], ['enabled', Boolean, false], ['at', Date, false], ['tags', Array, false], ['details', Object, false],
        ]);
        Reflect.set(proxy, 'title', 'First');
        proxy.hasChanges.should.equal(true);
    });
});
