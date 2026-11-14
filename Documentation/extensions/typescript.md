---
title: Write an extension backend in TypeScript
description: "Get started with the @cratis/stage-extensions SDK for Cratis Stage extension backends: declare abilities, return results, use settings and secrets, then test."
---

The TypeScript SDK is the package `@cratis/stage-extensions` (Node.js 20 or later, ES modules), at
[`Source/Extensions/TypeScript`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/TypeScript). It loads the shared `.proto` files with
`@grpc/proto-loader`, so there is no code generation step. Read the [protocol reference](protocol.md) first for what the results mean.

## Install

The package is not published to npm yet. Until then, build it from a clone of [Cratis/Stage](https://github.com/Cratis/Stage) and install the folder:

```bash
cd Stage/Source/Extensions/TypeScript && npm install && npm run build
cd ../../../../my-extension && npm install ../Stage/Source/Extensions/TypeScript
```

## The smallest extension

[`Source/Extensions/TypeScript/examples/hello.ts`](https://github.com/Cratis/Stage/blob/main/Source/Extensions/TypeScript/examples/hello.ts):

```ts
import { Extension, outcome } from '@cratis/stage-extensions';

const hello = Extension.create('hello', '1.0.0')
    .setting('greeting')
    .ability('Greet', 'safe', async call => {
        const greeting = (await call.host.setting('greeting')) ?? 'Hello';
        const { name } = JSON.parse(call.input) as { name: string };
        return outcome.returned(JSON.stringify({ message: `${greeting}, ${name}!` }));
    });

const { port } = await hello.listen('0.0.0.0:50051');
console.log(`hello extension listening on ${port}`);
```

Point Stage at `http://localhost:50051` ([connect a backend](connect-a-backend.md)).

## Declare what the extension realizes

Everything declared is sent at the handshake and **checked against the contract of the system**, so the effect class and the outcomes must be
what the Screenplay `system` declares.

| Call | Declares |
| --- | --- |
| `.ability(name, effect, handler, { outcomes, contractMajor })` | An ability. `effect` is `'safe'`, `'idempotent'` or `'once'`. |
| `.setting(name, required?)` | A public setting read with `host.setting`. |
| `.secret(name, required?)` | A secret read with `host.secret`. |
| `.trigger(name)` | A trigger raised with `host.trigger`. |
| `.http(prefix, handler)` | A handler for inbound HTTP below a path prefix, such as a webhook. |
| `.listen(address)` / `.createServer()` | Starts serving and resolves the chosen port, or returns the `grpc.Server` to serve yourself. |

## Answer an attempt

A handler returns an `AbilityOutcome`, directly or as a promise:

| Return | When |
| --- | --- |
| `outcome.returned(output?, named?)` | The system did the work. `output` is JSON; `named` is a declared outcome. |
| `outcome.accepted()` | The system took the work and will answer later. Keep `call.invocationId`. |
| `outcome.notSent(reason)` | The call certainly did not reach the system. |
| `outcome.failed(reason, effectRuledOut?)` | The call failed. Pass `true` only when you can prove the effect did not happen. |

A thrown error or a rejected promise becomes `failed` **without** ruling the effect out, which Stage treats as "may have happened": a `once`
ability then stops for reconciliation.

The context has `tenant`, `system`, `invocationId`, `idempotencyKey`, `attempt`, `deadline`, `causation`, `traceparent`, `input` and `host`.
Deduplicate your effect on `idempotencyKey`: it is the same on every attempt.

## Use the host services

```ts
const channel = await call.host.setting('channel');                  // public setting, undefined when not set
const token = await call.host.secret('token');                       // secret; keep it in memory only
const raised = await call.host.trigger('MessageReceived', payload);  // dedup key defaults to the idempotency key
await call.host.answer('approved', '{"by":"ada"}');                  // complete an accepted invocation later
await call.host.log('warning', 'rate limit close', { remaining: '3' });
await call.host.counter('messages.posted');
await call.host.histogram('post.duration', 0.25, 's');
```

`setting` and `secret` reject with a `HostServiceError` for a name the extension did not declare or the wrong kind. `trigger` resolves to a
`TriggerResult` whose `status` is `Raised`, `UnknownTrigger`, `NotRouted` or `Refused`. Details are in the [host services reference](host-services.md).

## Test

```bash
cd Source/Extensions/TypeScript && npm test
```

builds the package and runs the shared [conformance suite](conformance.md) against a reference extension built with the SDK, using Node's test runner.
