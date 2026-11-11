---
title: Write an extension backend in Kotlin
description: Getting started with the Kotlin SDK for Cratis Stage extension backends - declare abilities, return the right result, use settings, secrets, triggers, answers and telemetry, run the backend and test it.
---

The Kotlin SDK is `io.cratis.stage:stage-extensions` (JVM 17), at
[`Source/Extensions/Kotlin`](https://github.com/Cratis/Stage/tree/main/Source/Extensions/Kotlin). It is built on gRPC and kotlinx.coroutines, so
handlers and host services are `suspend` functions. Read the [protocol reference](protocol.md) first for what the results mean.

## Install

The SDK is a Gradle project that builds the protocol stubs from the shared `.proto` files. It is not published to a Maven repository yet. Until
then, depend on it from a clone of [Cratis/Stage](https://github.com/Cratis/Stage) with a composite build:

```kotlin
// settings.gradle.kts
includeBuild("../Stage/Source/Extensions/Kotlin")
```

```kotlin
// build.gradle.kts
dependencies {
    implementation("io.cratis.stage:stage-extensions")
}
```

## The smallest extension

[`Source/Extensions/Kotlin/examples/Hello.kt`](https://github.com/Cratis/Stage/blob/main/Source/Extensions/Kotlin/examples/Hello.kt):

```kotlin
fun main() {
    val hello = Extension.create("hello", "1.0.0")
        .setting("greeting")
        .ability("Greet", EffectClass.Safe) { call ->
            val greeting = call.host.setting("greeting") ?: "Hello"
            val name = JsonParser.parseString(call.input).asJsonObject["name"].asString
            val output = JsonObject()
            output.addProperty("message", "$greeting, $name!")
            AbilityOutcome.Returned(output.toString())
        }

    val server = hello.listen(50051).start()
    println("hello extension listening on ${server.port}")
    server.awaitTermination()
}
```

Run it with `gradle runExample`, then point Stage at `http://localhost:50051`
([connect a backend](connect-a-backend.md)).

## Declare what the extension realizes

Everything declared is sent at the handshake and **checked against the contract of the system**, so the effect class and the outcomes must be
what the Screenplay `system` declares.

| Call | Declares |
| --- | --- |
| `.ability(name, effect, outcomes = listOf(), contractMajor = 1) { call -> … }` | An ability. `effect` is `EffectClass.Safe`, `Idempotent` or `Once`. |
| `.setting(name, required = false)` | A public setting read with `host.setting`. |
| `.secret(name, required = false)` | A secret read with `host.secret`. |
| `.trigger(name)` | A trigger raised with `host.trigger`. |
| `.http(prefix) { call -> … }` | A handler for inbound HTTP below a path prefix, such as a webhook. |
| `.listen(port, host = "0.0.0.0")` | The gRPC `Server`; call `start()` on it. |

## Answer an attempt

A handler is `suspend (AbilityContext) -> AbilityOutcome`:

| Return | When |
| --- | --- |
| `AbilityOutcome.Returned(output = "{}", outcome = "")` | The system did the work. `outcome` names a declared outcome. |
| `AbilityOutcome.Accepted` | The system took the work and will answer later. Keep `call.invocationId`. |
| `AbilityOutcome.NotSent(reason)` | The call certainly did not reach the system. |
| `AbilityOutcome.Failed(reason, effectRuledOut = false)` | The call failed. Set `effectRuledOut` only when you can prove the effect did not happen. |

An exception becomes `Failed` **without** ruling the effect out, which Stage treats as "may have happened": a `once` ability then stops for
reconciliation. Cancellation is not turned into a failure.

The context has `tenant`, `system`, `invocationId`, `idempotencyKey`, `attempt`, `deadline`, `causation`, `traceparent` and `input`. Deduplicate
your effect on `idempotencyKey`: it is the same on every attempt.

## Use the host services

```kotlin
val channel = call.host.setting("channel")          // public setting, null when not set
val token = call.host.secret("token")               // secret; keep it in memory only
val raised = call.host.trigger("MessageReceived", payloadJson)   // dedup key defaults to the idempotency key
call.host.answer("approved", """{"by":"ada"}""")    // complete an accepted invocation later
call.host.log(Level.Warning, "rate limit close", mapOf("remaining" to "3"))
call.host.counter("messages.posted")
call.host.histogram("post.duration", 0.25, "s")
```

`setting` and `secret` throw `HostServiceException` for a name the extension did not declare or the wrong kind. `trigger` returns a `TriggerResult`
with `status` `Raised`, `UnknownTrigger`, `NotRouted` or `Refused`. Details are in the [host services reference](host-services.md).

## Test

```bash
cd Source/Extensions/Kotlin && gradle test
```

runs the shared [conformance suite](conformance.md) against a reference extension built with the SDK.
