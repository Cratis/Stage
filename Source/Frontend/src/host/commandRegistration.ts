// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { registerCommand } from '@cratis/scene.components';
import type { StageRoutes } from '../stageRoutes';

/** Registers the live host's commands before rendering. Generated apps register generated proxies instead. */
export function registerStageCommands(routes: StageRoutes): void {
    for (const [name, route] of Object.entries(routes.commands)) {
        const schema = routes.commandSchemas?.[name];
        if (!schema) {
            console.warn(`Command metadata for '${name}' is missing; Stage cannot register its proxy.`);
            continue;
        }
        const command = commandTypeFor(route, schema);
        registerCommand(name, command);
        registerCommand(route.replace(/^\//, ''), command);
    }
}

function commandTypeFor(route: string, schema: string): new () => Command {
    const descriptors = descriptorsFor(schema);
    return class StageNativeCommand extends Command {
        readonly route = route.replace(/^\//, '');
        readonly propertyDescriptors = descriptors;
        get requestParameters(): string[] { return []; }

        constructor() {
            super(Object, false);
            const values: Record<string, unknown> = {};
            for (const descriptor of descriptors) {
                Object.defineProperty(this, descriptor.name, {
                    configurable: true,
                    enumerable: true,
                    get: () => values[descriptor.name],
                    set: (value: unknown) => {
                        values[descriptor.name] = value;
                        this.propertyChanged(descriptor.name);
                    },
                });
            }
        }
    };
}

function descriptorsFor(schema: string): PropertyDescriptor[] {
    const parsed: unknown = JSON.parse(schema);
    if (!isRecord(parsed) || !isRecord(parsed.properties)) throw new Error('Command schema must declare its properties.');
    const required = Array.isArray(parsed.required) ? parsed.required : [];
    return Object.entries(parsed.properties).map(([name, definition]) =>
        new PropertyDescriptor(name, constructorFor(definition), !required.includes(name)));
}

function constructorFor(definition: unknown) {
    if (!isRecord(definition)) return Object;
    const type = Array.isArray(definition.type) ? definition.type.find(type => type !== 'null') : definition.type;
    if (type === 'integer' || type === 'number') return Number;
    if (type === 'boolean') return Boolean;
    if (type === 'array') return Array;
    if (definition.format === 'date' || definition.format === 'date-time') return Date;
    if (type === 'string') return String;
    return Object;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}
