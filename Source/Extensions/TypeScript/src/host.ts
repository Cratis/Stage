// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as grpc from '@grpc/grpc-js';

/** The level of a log record. */
export type LogLevel = 'trace' | 'debug' | 'information' | 'warning' | 'error' | 'critical';

/** What came of raising a trigger. */
export interface TriggerResult {
    /** `Raised`, `UnknownTrigger`, `NotRouted` (nothing in the model reacts to it) or `Refused`. */
    status: string;
    detail: string;
    raised: boolean;
}

/** A host service refused what the extension asked for, or was used before the handshake. */
export class HostServiceError extends Error {
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type Client = any;

/** What the host services of one handshake are reached through. @internal */
export interface HostConnection {
    client: Client | undefined;
    credential: string;
}

/**
 * What Stage offers an extension: settings and secrets, triggers, answering later, and logs and metrics tied to the
 * invocation they are used from.
 */
export class HostServices {
    /** @internal */
    constructor(private readonly connection: HostConnection, private readonly context: Record<string, unknown>) {
    }

    /** Reads a public setting the extension declared. Resolves to `undefined` when it is not set. */
    async setting(name: string): Promise<string | undefined> {
        const response = await this.call('getSetting', { context: this.context, name });
        switch (response.status) {
            case 'STATUS_FOUND': return response.value as string;
            case 'STATUS_NOT_SET': return undefined;
            case 'STATUS_IS_SECRET': throw new HostServiceError(`${name} is declared as a secret: read it with secret().`);
            default: throw new HostServiceError(`${name} is not a setting the extension declared.`);
        }
    }

    /** Reads a secret the extension declared. Do not log it or return it from an ability. */
    async secret(name: string): Promise<string | undefined> {
        const response = await this.call('getSecret', { context: this.context, name });
        switch (response.status) {
            case 'STATUS_FOUND': return response.value as string;
            case 'STATUS_NOT_SET': return undefined;
            case 'STATUS_NOT_SECRET': throw new HostServiceError(`${name} is declared as a public setting: read it with setting().`);
            default: throw new HostServiceError(`${name} is not a secret the extension declared.`);
        }
    }

    /**
     * Raises a trigger the extension declared. The deduplication key makes raising it again find the first raise; within
     * an invocation it defaults to the idempotency key, outside one it is required.
     */
    async trigger(trigger: string, payloadJson = '{}', deduplicationKey?: string): Promise<TriggerResult> {
        const key = deduplicationKey ?? (this.context.idempotencyKey as string | undefined);
        if (!key) {
            throw new HostServiceError('A trigger raised outside an invocation needs a deduplication key.');
        }
        const response = await this.call('raiseTrigger', { context: this.context, trigger, deduplicationKey: key, payloadJson });
        const status = pascal(response.status as string);
        return { status, detail: (response.detail as string | undefined) ?? '', raised: status === 'Raised' };
    }

    /**
     * Completes an invocation the extension accepted to answer later. The outlet is `returned` or a declared outcome. It
     * defaults to the invocation this was obtained for. Resolves to the status of the answer, `Answered` when it was taken.
     */
    async answer(outlet: string, payloadJson = '{}', invocationId?: string): Promise<string> {
        const context = { ...this.context, invocationId: invocationId ?? this.context.invocationId };
        const response = await this.call('answer', { context, outlet, payloadJson });
        return pascal(response.status as string);
    }

    /** Writes a log record tied to the invocation. Never put a secret in it. */
    async log(level: LogLevel, message: string, attributes: Record<string, string> = {}): Promise<void> {
        const now = Date.now();
        const record = {
            level: `LOG_LEVEL_${level.toUpperCase()}`,
            message,
            occurred: { seconds: String(Math.floor(now / 1000)), nanos: (now % 1000) * 1_000_000 },
            attributes: pairs(attributes)
        };
        await this.call('log', { context: this.context, records: [record] });
    }

    /** Adds to a counter tied to the invocation. */
    async counter(name: string, value = 1, attributes: Record<string, string> = {}): Promise<void> {
        await this.call('recordMetrics', { context: this.context, metrics: [{ name, kind: 'KIND_COUNTER', value, attributes: pairs(attributes) }] });
    }

    /** Records one measurement of a distribution, in a unit such as `s`. */
    async histogram(name: string, value: number, unit = '', attributes: Record<string, string> = {}): Promise<void> {
        await this.call('recordMetrics', { context: this.context, metrics: [{ name, kind: 'KIND_HISTOGRAM', value, unit, attributes: pairs(attributes) }] });
    }

    private call(method: string, request: object): Promise<Record<string, unknown>> {
        const client = this.connection.client;
        if (!client) {
            return Promise.reject(new HostServiceError('Stage has not completed the handshake with this extension, so the host services are not reachable yet.'));
        }
        const metadata = new grpc.Metadata();
        metadata.set('authorization', `Bearer ${this.connection.credential}`);
        return new Promise((resolve, reject) => {
            client[method](request, metadata, (error: Error | null, response: Record<string, unknown>) => error ? reject(error) : resolve(response));
        });
    }
}

function pairs(attributes: Record<string, string>): { key: string; value: string }[] {
    return Object.entries(attributes).map(([key, value]) => ({ key, value }));
}

/** Turns STATUS_UNKNOWN_TRIGGER into UnknownTrigger. */
function pascal(status: string): string {
    return status.replace(/^STATUS_/, '').toLowerCase().split('_').map(part => part.charAt(0).toUpperCase() + part.slice(1)).join('');
}
