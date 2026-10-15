// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { InvocationResult, SliceCommandField, SliceInvoker } from '@cratis/scene.react';
import { schemaPropertiesOf } from './StageCommandForm';
import { executeStageCommand } from './stageCommands';
import type { StageRoutes } from './stageRoutes';

/**
 * The fields of a command, for the task dialog a toolbar slice button opens. They are what the command's schema declares -
 * the same reading the native command form uses - so the dialog asks for exactly what the command takes. A property the form
 * cannot present (a collection, an object) is left out rather than shown as a field that cannot be filled.
 *
 * @param schemas The JSON schema of every modeled command, by command name.
 * @returns The fields of a command; none for a command the application does not describe.
 */
export function sliceFieldsFrom(schemas: Record<string, string> | undefined): (command: string) => SliceCommandField[] {
    return command => schemaPropertiesOf(schemas?.[command] ?? '')
        .filter(property => property.unsupportedReason === undefined)
        .map(property => ({
            name: property.name,
            type: property.type === 'text' || property.type === undefined ? 'string' : property.type,
            label: humanize(property.name),
            required: property.required,
        }));
}

function humanize(name: string): string {
    return name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/^./, first => first.toUpperCase());
}

/**
 * The invoker the running application submits a task dialog through: the command is posted to the route the Stage registered for
 * it, exactly as an action or a form posts it, and the application's own validation results come back to the dialog against the
 * fields they name. A successful command refreshes what the screen shows.
 *
 * @param routes What the Stage registered, once it is known; undefined while it is still loading.
 * @param routesReady Whether the lookup has finished, so a command with no route is not-registered rather than not-yet.
 * @param refreshAfterCommand Called after a command succeeds.
 */
export function createStageSliceInvoker(routes: StageRoutes | undefined, routesReady: boolean, refreshAfterCommand: () => void): SliceInvoker {
    return {
        invoke: async (sliceId, payload, command): Promise<InvocationResult> => {
            const name = command ?? sliceId.slice(sliceId.lastIndexOf('.') + 1);
            const route = routes?.commands[name];
            if (!route) {
                return { succeeded: false, message: routesReady ? `The modeled command “${name}” is not registered by this Stage.` : `The modeled command “${name}” is waiting for Stage routes.` };
            }

            const outcome = await executeStageCommand(name, route, payload);
            if (outcome.isSuccess) refreshAfterCommand();
            return {
                succeeded: outcome.isSuccess,
                ...(outcome.validationErrors.length > 0 ? { validationErrors: outcome.validationErrors.map(error => ({ field: error.member || undefined, message: error.message })) } : {}),
                ...(outcome.messages.length > 0 ? { message: outcome.messages.join(' ') } : {}),
                ...(outcome.isSuccess ? { value: outcome.response } : {}),
            };
        },
    };
}
