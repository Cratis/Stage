// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as grpc from '@grpc/grpc-js';
import { HostConnection, HostServices } from './host.js';
import { AbilityOutcome, EffectClass, effectNames, outcome, toInvokeResponse } from './outcome.js';
import { protocol, protocolVersion } from './protocol.js';

/** One attempt at an ability, as the handler receives it. */
export interface AbilityContext {
    /** The tenant the work runs for. */
    tenant: string;
    /** The system whose ability is performed. */
    system: string;
    /** The invocation identity. */
    invocationId: string;
    /** The key the effect is deduplicated on. It is the same for every attempt of the invocation. */
    idempotencyKey: string;
    /** The attempt number, starting at 1. */
    attempt: number;
    /** When the attempt runs out of time, if a deadline was imposed. */
    deadline?: Date;
    /** What caused the invocation. */
    causation: string;
    /** The W3C trace context, so the extension's own telemetry joins the invocation's trace. */
    traceparent: string;
    /** The ability's input, as JSON. */
    input: string;
    /** The host services, tied to this invocation. */
    host: HostServices;
}

/** Performs one attempt at an ability. A thrown error is a failure that does not rule the effect out. */
export type AbilityHandler = (call: AbilityContext) => Promise<AbilityOutcome> | AbilityOutcome;

/** An inbound HTTP request Stage routed to the extension. Cookies and authorization are never forwarded. */
export interface HttpCall {
    method: string;
    path: string;
    query: string;
    headers: Record<string, string[]>;
    body: Buffer;
}

/** The answer to an {@link HttpCall}. */
export interface HttpReply {
    status: number;
    body?: Buffer | string;
    headers?: Record<string, string[]>;
}

/** Handles an inbound HTTP request. */
export type HttpHandler = (call: HttpCall) => Promise<HttpReply> | HttpReply;

interface Ability {
    name: string;
    effect: EffectClass;
    outcomes: string[];
    contractMajor: number;
    handler: AbilityHandler;
}

interface Setting {
    name: string;
    secret: boolean;
    required: boolean;
}

/** Describes an extension backend: what it realizes and how. Declare it, then {@link Extension.listen} for Stage to connect. */
export class Extension {
    private readonly abilities: Ability[] = [];
    private readonly triggers: string[] = [];
    private readonly settings: Setting[] = [];
    private readonly routes: { prefix: string; handler: HttpHandler }[] = [];
    private readonly connection: HostConnection = { client: undefined, credential: '' };
    private hostChannel: grpc.Client | undefined;

    private constructor(private readonly name: string, private readonly version: string) {
    }

    /** Starts describing an extension. */
    static create(name: string, version: string): Extension {
        return new Extension(name, version);
    }

    /**
     * Declares an ability the extension realizes. Stage checks it against the contract of the system at the handshake, so
     * the effect class and outcomes must be what the contract declares.
     */
    ability(name: string, effect: EffectClass, handler: AbilityHandler, options: { outcomes?: string[]; contractMajor?: number } = {}): this {
        const index = this.abilities.findIndex(existing => existing.name === name);
        if (index >= 0) this.abilities.splice(index, 1);
        this.abilities.push({ name, effect, handler, outcomes: options.outcomes ?? [], contractMajor: options.contractMajor ?? 1 });
        return this;
    }

    /** Declares a trigger the extension raises. */
    trigger(name: string): this {
        this.triggers.push(name);
        return this;
    }

    /** Declares a public setting the extension reads. */
    setting(name: string, required = false): this {
        this.settings.push({ name, secret: false, required });
        return this;
    }

    /** Declares a secret the extension reads. It is delivered by the host services and nowhere else. */
    secret(name: string, required = false): this {
        this.settings.push({ name, secret: true, required });
        return this;
    }

    /** Handles inbound HTTP requests below a path prefix, such as a webhook. */
    http(prefix: string, handler: HttpHandler): this {
        this.routes.push({ prefix, handler });
        return this;
    }

    /** Creates the gRPC server that speaks the extension protocol for the extension. */
    createServer(): grpc.Server {
        const server = new grpc.Server();
        server.addService(protocol.ExtensionBackend.service, {
            describe: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => this.describe(call, callback), // eslint-disable-line @typescript-eslint/no-explicit-any
            invoke: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => void this.invoke(call, callback), // eslint-disable-line @typescript-eslint/no-explicit-any
            handleHttp: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => void this.handleHttp(call, callback), // eslint-disable-line @typescript-eslint/no-explicit-any
            health: (_call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => callback(null, { status: 'STATUS_SERVING' }) // eslint-disable-line @typescript-eslint/no-explicit-any
        });
        return server;
    }

    /** Starts serving on an address such as `0.0.0.0:50051`. Use port 0 to let the operating system choose. */
    async listen(address: string): Promise<{ server: grpc.Server; port: number }> {
        const server = this.createServer();
        const port = await new Promise<number>((resolve, reject) =>
            server.bindAsync(address, grpc.ServerCredentials.createInsecure(), (error, bound) => error ? reject(error) : resolve(bound)));
        return { server, port };
    }

    private describe(call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>): void { // eslint-disable-line @typescript-eslint/no-explicit-any
        const request = call.request;
        if (request.protocolVersion?.major !== protocolVersion.major) {
            callback({ code: grpc.status.FAILED_PRECONDITION, details: `This SDK speaks protocol major ${protocolVersion.major}; Stage speaks ${request.protocolVersion?.major}.` });
            return;
        }

        // The credential arrives here and is kept in memory: nowhere else, never in the environment or a file.
        this.hostChannel?.close();
        this.hostChannel = undefined;
        this.connection.client = undefined;
        if (request.hostEndpoint) {
            const target = (request.hostEndpoint as string).replace(/^https?:\/\//, '');
            this.connection.client = new protocol.StageHost(target, grpc.credentials.createInsecure());
            this.hostChannel = this.connection.client;
        }
        this.connection.credential = request.sessionToken ?? '';

        callback(null, {
            protocolVersion: protocolVersion,
            name: this.name,
            version: this.version,
            abilities: this.abilities.map(ability => ({ name: ability.name, contractMajor: ability.contractMajor, effect: effectNames[ability.effect], outcomes: ability.outcomes })),
            triggers: this.triggers.map(name => ({ name })),
            settings: this.settings,
            httpRoutes: this.routes.map(route => route.prefix)
        });
    }

    private async invoke(call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>): Promise<void> { // eslint-disable-line @typescript-eslint/no-explicit-any
        const request = call.request;
        const ability = this.abilities.find(candidate => candidate.name === request.ability);
        if (!ability || ability.contractMajor !== request.contractMajor) {
            callback(null, toInvokeResponse(outcome.notSent(`The extension does not realize ${request.ability}@${request.contractMajor}.`)));
            return;
        }

        const context = request.context ?? {};
        const deadline = context.deadline ? new Date(Number(context.deadline.seconds) * 1000 + Math.floor((context.deadline.nanos ?? 0) / 1_000_000)) : undefined;
        try {
            const result = await ability.handler({
                tenant: context.tenant ?? '',
                system: context.system ?? '',
                invocationId: context.invocationId ?? '',
                idempotencyKey: context.idempotencyKey ?? '',
                attempt: context.attempt ?? 0,
                deadline,
                causation: context.causation ?? '',
                traceparent: context.traceparent ?? '',
                input: request.inputJson ?? '',
                host: new HostServices(this.connection, context)
            });
            callback(null, toInvokeResponse(result));
        } catch (error) {
            // An error says nothing about whether the effect happened, so it is not ruled out.
            callback(null, toInvokeResponse(outcome.failed(error instanceof Error ? error.message : String(error))));
        }
    }

    private async handleHttp(call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>): Promise<void> { // eslint-disable-line @typescript-eslint/no-explicit-any
        const request = call.request;
        const route = this.routes
            .filter(candidate => request.path === candidate.prefix || (request.path as string).startsWith(candidate.prefix.replace(/\/+$/, '') + '/'))
            .sort((first, second) => second.prefix.length - first.prefix.length)[0];
        if (!route) {
            callback(null, { status: 404 });
            return;
        }

        const headers: Record<string, string[]> = {};
        for (const header of request.headers ?? []) headers[header.name] = header.values ?? [];
        try {
            const reply = await route.handler({ method: request.method, path: request.path, query: request.query ?? '', headers, body: request.body ?? Buffer.alloc(0) });
            callback(null, {
                status: reply.status,
                body: typeof reply.body === 'string' ? Buffer.from(reply.body) : reply.body ?? Buffer.alloc(0),
                headers: Object.entries(reply.headers ?? {}).map(([name, values]) => ({ name, values }))
            });
        } catch {
            callback(null, { status: 500 });
        }
    }
}
