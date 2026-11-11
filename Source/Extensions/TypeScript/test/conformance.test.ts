// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import * as grpc from '@grpc/grpc-js';
import { Extension, outcome } from '../src/index.js';
import { protocol } from '../src/protocol.js';

/**
 * The reference extension of the conformance suite, written with the TypeScript SDK. Every SDK implements the same
 * behavior; see Source/Extensions/Conformance/README.md.
 */
function conformanceExtension(): Extension {
    return Extension.create('conformance-extension', '1.0.0')
        .setting('greeting', true)
        .secret('apiKey', true)
        .trigger('Notified')
        .ability('Echo', 'safe', async call => {
            await call.host.log('information', 'echoing', { size: String(Buffer.byteLength(call.input)) });
            await call.host.counter('echo.calls', 1);
            return outcome.returned(call.input);
        })
        .ability('Greet', 'idempotent', async call => {
            const greeting = await call.host.setting('greeting');
            const key = await call.host.secret('apiKey');
            return outcome.returned(JSON.stringify({ greeting, secretLength: key?.length }), 'greeted');
        }, { outcomes: ['greeted'] })
        .ability('Context', 'safe', call => outcome.returned(JSON.stringify({
            tenant: call.tenant,
            system: call.system,
            invocationId: call.invocationId,
            idempotencyKey: call.idempotencyKey,
            attempt: call.attempt,
            causation: call.causation,
            traceparent: call.traceparent
        })))
        .ability('Notify', 'once', async call => {
            await call.host.trigger('Notified', call.input);
            return outcome.returned();
        })
        .ability('Defer', 'once', call => {
            // Accept now, answer later: the answer is given after the attempt has been acknowledged.
            setTimeout(() => void call.host.answer('done', '{"ok":true}'), 50);
            return outcome.accepted();
        }, { outcomes: ['done'] })
        .ability('Refuse', 'idempotent', () => outcome.notSent('refused by design'))
        .ability('Reject', 'once', () => outcome.failed('rejected by the system', true))
        .ability('Crash', 'once', () => { throw new Error('boom'); })
        .http('/hook', call => call.path === '/hook/events'
            ? { status: 202, body: Buffer.concat([Buffer.from('received:'), call.body]), headers: { 'X-Echo': [call.query] } }
            : { status: 404 });
}

interface RecordedCall {
    rpc: string;
    request: unknown;
    authorization: string | undefined;
}

interface Document {
    token: string;
    host: { settings: Record<string, string>; secrets: Record<string, string> };
    cases: {
        name: string;
        rpc: string;
        request: Record<string, unknown>;
        expect: unknown;
        hostCalls?: { rpc: string; request: unknown }[];
    }[];
}

/** Stage's side of the protocol as the conformance suite fakes it: it records every call with the credential it carried. */
async function startFakeHost(document: Document, calls: RecordedCall[]): Promise<{ server: grpc.Server; address: string }> {
    const server = new grpc.Server();
    const record = (rpc: string, call: grpc.ServerUnaryCall<any, any>): void => { // eslint-disable-line @typescript-eslint/no-explicit-any
        calls.push({ rpc, request: call.request, authorization: call.metadata.get('authorization')[0]?.toString() });
    };
    server.addService(protocol.StageHost.service, {
        getSetting: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => { // eslint-disable-line @typescript-eslint/no-explicit-any
            record('GetSetting', call);
            const value = document.host.settings[call.request.name];
            callback(null, value === undefined ? { status: 'STATUS_NOT_SET' } : { status: 'STATUS_FOUND', value });
        },
        getSecret: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => { // eslint-disable-line @typescript-eslint/no-explicit-any
            record('GetSecret', call);
            const value = document.host.secrets[call.request.name];
            callback(null, value === undefined ? { status: 'STATUS_NOT_SET' } : { status: 'STATUS_FOUND', value });
        },
        raiseTrigger: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => { record('RaiseTrigger', call); callback(null, { status: 'STATUS_RAISED' }); }, // eslint-disable-line @typescript-eslint/no-explicit-any
        answer: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => { record('Answer', call); callback(null, { status: 'STATUS_ANSWERED' }); }, // eslint-disable-line @typescript-eslint/no-explicit-any
        log: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => { record('Log', call); callback(null, {}); }, // eslint-disable-line @typescript-eslint/no-explicit-any
        recordMetrics: (call: grpc.ServerUnaryCall<any, any>, callback: grpc.sendUnaryData<any>) => { record('RecordMetrics', call); callback(null, {}); } // eslint-disable-line @typescript-eslint/no-explicit-any
    });
    const port = await new Promise<number>((resolve, reject) => server.bindAsync('127.0.0.1:0', grpc.ServerCredentials.createInsecure(), (error, bound) => error ? reject(error) : resolve(bound)));
    return { server, address: `127.0.0.1:${port}` };
}

/** Turns what the cases say in protocol JSON into what the loader sends: a deadline becomes seconds and nanos, bytes become a buffer. */
function toWire(value: unknown, key = ''): unknown {
    if (Array.isArray(value)) return value.map(item => toWire(item, key));
    if (value !== null && typeof value === 'object') {
        return Object.fromEntries(Object.entries(value).map(([name, child]) => [name, toWire(child, name)]));
    }
    if (key === 'deadline' && typeof value === 'string') {
        const milliseconds = Date.parse(value);
        return { seconds: String(Math.floor(milliseconds / 1000)), nanos: 0 };
    }
    if (key === 'body' && typeof value === 'string') return Buffer.from(value, 'base64');
    return value;
}

/** Turns what the loader returns into protocol JSON again: buffers become base64. */
function fromWire(value: unknown): unknown {
    if (Buffer.isBuffer(value)) return value.toString('base64');
    if (Array.isArray(value)) return value.map(fromWire);
    if (value !== null && typeof value === 'object') {
        return Object.fromEntries(Object.entries(value).map(([name, child]) => [name, fromWire(child)]));
    }
    return value;
}

/** Everything expected is in actual: objects by key, arrays by index and length, scalars by value, and fields named *Json as JSON. */
function subset(expected: unknown, actual: unknown, path: string, failures: string[], name: string): void {
    if (Array.isArray(expected)) {
        if (!Array.isArray(actual) || actual.length !== expected.length) {
            failures.push(`${name}: ${path} does not have ${expected.length} items.`);
            return;
        }
        expected.forEach((item, index) => subset(item, actual[index], `${path}[${index}]`, failures, name));
    } else if (expected !== null && typeof expected === 'object') {
        if (actual === null || typeof actual !== 'object') {
            failures.push(`${name}: ${path} is not an object.`);
            return;
        }
        for (const [key, value] of Object.entries(expected)) subset(value, (actual as Record<string, unknown>)[key], `${path}.${key}`, failures, name);
    } else {
        if (typeof expected === 'string' && typeof actual === 'string' && path.endsWith('Json')) {
            try {
                assert.deepEqual(JSON.parse(actual), JSON.parse(expected));
                return;
            } catch {
                failures.push(`${name}: ${path} was ${actual}, expected ${expected}.`);
                return;
            }
        }
        if (expected !== actual) failures.push(`${name}: ${path} was ${String(actual)}, expected ${String(expected)}.`);
    }
}

test('the TypeScript SDK passes the conformance suite', async () => {
    const document = JSON.parse(readFileSync(fileURLToPath(new URL('../../../Conformance/cases.json', import.meta.url)), 'utf8')) as Document;
    assert.ok(document.cases.length > 10, 'expected the conformance cases');

    const calls: RecordedCall[] = [];
    const host = await startFakeHost(document, calls);
    const { server, port } = await conformanceExtension().listen('127.0.0.1:0');
    const client = new protocol.ExtensionBackend(`127.0.0.1:${port}`, grpc.credentials.createInsecure());
    const failures: string[] = [];

    try {
        for (const testCase of document.cases) {
            calls.length = 0;
            const request = toWire(JSON.parse(JSON.stringify(testCase.request).replace('@host', `http://${host.address}`)));
            const method = testCase.rpc[0].toLowerCase() + testCase.rpc.slice(1);
            let response: unknown;
            try {
                response = await new Promise((resolve, reject) =>
                    client[method](request, (error: Error | null, result: unknown) => error ? reject(error) : resolve(result)));
            } catch (error) {
                failures.push(`${testCase.name}: ${String(error)}`);
                continue;
            }
            subset(testCase.expect, fromWire(response), 'response', failures, testCase.name);

            if (testCase.hostCalls) {
                const deadline = Date.now() + 5000;
                while (calls.length < testCase.hostCalls.length && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 20));
                if (calls.length !== testCase.hostCalls.length) {
                    failures.push(`${testCase.name}: expected ${testCase.hostCalls.length} host calls, saw ${calls.length}.`);
                    continue;
                }
                testCase.hostCalls.forEach((expected, index) => {
                    if (calls[index].rpc !== expected.rpc) failures.push(`${testCase.name}: host call ${index} was ${calls[index].rpc}, expected ${expected.rpc}.`);
                    if (calls[index].authorization !== `Bearer ${document.token}`) failures.push(`${testCase.name}: host call ${index} did not present the session credential.`);
                    subset(expected.request, fromWire(calls[index].request), `host call ${index}`, failures, testCase.name);
                });
            }
        }
    } finally {
        client.close();
        server.forceShutdown();
        host.server.forceShutdown();
    }

    assert.deepEqual(failures, [], `the TypeScript SDK failed ${failures.length} conformance checks`);
});
