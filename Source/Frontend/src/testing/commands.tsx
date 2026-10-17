// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ExternalComponent } from '@cratis/scene.model';
import { registerStageCommands } from '../host/commandRegistration';
import { StageCommandForm } from '../StageCommandForm';
import { StageAction } from '../stageComponents';
import { useStageData } from '../stageData';

/** A complete Arc server result; raw-fetch specs previously needed only isSuccess. */
export function commandResult(overrides: Record<string, unknown> = {}) {
    return {
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false,
        validationResults: [], exceptionMessages: [], exceptionStackTrace: '', authorizationFailureReason: '', response: null,
        ...overrides,
    };
}

/** Registers a native proxy for a fixture's command, just as the live host does before rendering. */
export function registerTestCommand(name: string, route: string, properties: Record<string, object> = {}, required = Object.keys(properties)) {
    registerStageCommands({ commands: { [name]: route }, queries: {}, commandSchemas: { [name]: JSON.stringify({ type: 'object', properties, required }) } });
}

function registerForm(element: ExternalComponent, routes: Record<string, string> | undefined) {
    const name = element.properties.command;
    if (typeof name !== 'string') return;
    const route = routes?.[name] ?? (typeof element.properties.route === 'string' ? element.properties.route : undefined);
    if (!route) return;
    const schema = element.properties.schema;
    if (typeof schema === 'string') {
        registerStageCommands({ commands: { [name]: route }, queries: {}, commandSchemas: { [name]: schema } });
        return;
    }
    const fields = Array.isArray(element.properties.fields) ? element.properties.fields : [];
    const properties: Record<string, object> = {};
    for (const field of fields) {
        if (typeof field.name === 'string') properties[field.sourceProperty ?? field.name] = { type: 'string' };
    }
    registerTestCommand(name, route, properties);
}

/** Fixture setup for directly rendered forms; production registration is exercised separately. */
export function RegisteredForm(props: Parameters<typeof StageCommandForm>[0]) {
    registerForm(props.element, useStageData().routes?.commands);
    return <StageCommandForm {...props} />;
}

/** Fixture setup for directly rendered command actions. */
export function RegisteredAction(props: Parameters<typeof StageAction>[0]) {
    registerForm(props.element, useStageData().routes?.commands);
    return <StageAction {...props} />;
}
