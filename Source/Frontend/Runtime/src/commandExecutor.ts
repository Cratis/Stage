// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useContext } from 'react';
import { ArcContext, type ArcConfiguration } from '@cratis/arc.react';
import { Command, type ICommandResult } from '@cratis/arc/commands';
import { resolveCommand } from '@cratis/scene.components';
import type { StageCommandOutcome } from './StageCommandOutcome';

/** Resolves only native Arc command proxies; unrelated registry entries cannot execute. */
export function registeredCommand(name: string, typeName?: string, route?: string): (new () => Command) | undefined {
    for (const identity of [typeName, route?.replace(/^\//, ''), name]) {
        if (!identity) continue;
        const candidate = resolveCommand(identity);
        if (!isCommand(candidate)) continue;
        // Generated bindings use simple names. A planned route still must select that same proxy.
        if (identity === name && route && new candidate().route.replace(/^\//, '') !== route.replace(/^\//, '')) continue;
        return candidate;
    }
    return undefined;
}

function isCommand(candidate: unknown): candidate is new () => Command {
    return typeof candidate === 'function' && candidate.prototype instanceof Command;
}

/**
 * The path the page is served under, without a trailing slash: empty at the root of a host, `/api/play/{id}` when a
 * reverse proxy (Studio's Play) hangs the Stage off a prefix. Registered command routes start with `/`, so without this
 * they would be posted to the root of the proxy's host instead of to the Stage.
 */
export function hostedBasePath(): string {
    const document = globalThis.document;
    const address = document?.baseURI ?? globalThis.location?.href;
    if (!address) return '';
    try {
        return new URL('.', address).pathname.replace(/\/+$/, '');
    } catch {
        return '';
    }
}

/** Executes the registered proxy with the same host configuration as Arc's useCommand. */
export async function executeRegisteredCommand(name: string, values: Record<string, unknown>, arc: ArcConfiguration): Promise<StageCommandOutcome> {
    const commandType = registeredCommand(name);
    if (!commandType) return { isSuccess: false, validationErrors: [], messages: [`The modeled command “${name}” is not registered by this Stage.`] };

    const command = new commandType();
    command.setMicroservice(arc.microservice);
    command.setApiBasePath(arc.apiBasePath ?? hostedBasePath());
    command.setOrigin(arc.origin ?? '');
    command.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
    for (const descriptor of command.propertyDescriptors) {
        if (Object.prototype.hasOwnProperty.call(values, descriptor.name)) Reflect.set(command, descriptor.name, values[descriptor.name]);
    }

    return commandOutcome(name, await command.execute());
}

/** Maps Arc's typed result without treating a rejection or authorization failure as success. */
export function commandOutcome(name: string, result: ICommandResult): StageCommandOutcome {
    const isSuccess = result.isSuccess && result.isAuthorized && result.isValid && !result.hasExceptions;
    const validationErrors = result.isValid ? [] : result.validationResults.flatMap(validation =>
        (validation.members.length > 0 ? validation.members : ['']).map(member => ({ member, message: validation.message })));
    const messages = isSuccess ? [] : [
        ...result.validationResults.map(validation => validation.message),
        ...result.exceptionMessages,
        ...(!result.isAuthorized ? [result.authorizationFailureReason || `“${name}” was not accepted.`] : []),
    ];
    return {
        isSuccess,
        response: isSuccess ? result.response : undefined,
        validationErrors,
        messages: isSuccess || messages.length > 0 ? messages : [`“${name}” was not accepted.`],
    };
}

/** The command executor for interactions, guarded actions and slice task dialogs. */
export function useStageCommandExecutor() {
    const arc = useContext(ArcContext);
    return useCallback((name: string, values: Record<string, unknown>) => executeRegisteredCommand(name, values, arc), [arc]);
}
