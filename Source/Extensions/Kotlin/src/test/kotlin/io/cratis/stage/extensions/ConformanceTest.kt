// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.stage.extensions

import com.google.gson.JsonArray
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import com.google.protobuf.Message
import com.google.protobuf.util.JsonFormat
import io.cratis.stage.extensions.protocol.AnswerRequest
import io.cratis.stage.extensions.protocol.AnswerResponse
import io.cratis.stage.extensions.protocol.DescribeRequest
import io.cratis.stage.extensions.protocol.ExtensionBackendGrpcKt
import io.cratis.stage.extensions.protocol.GetSecretRequest
import io.cratis.stage.extensions.protocol.GetSecretResponse
import io.cratis.stage.extensions.protocol.GetSettingRequest
import io.cratis.stage.extensions.protocol.GetSettingResponse
import io.cratis.stage.extensions.protocol.HealthRequest
import io.cratis.stage.extensions.protocol.InboundHttpRequest
import io.cratis.stage.extensions.protocol.InvokeRequest
import io.cratis.stage.extensions.protocol.LogRequest
import io.cratis.stage.extensions.protocol.MetricsRequest
import io.cratis.stage.extensions.protocol.RaiseTriggerRequest
import io.cratis.stage.extensions.protocol.RaiseTriggerResponse
import io.cratis.stage.extensions.protocol.StageHostGrpcKt
import io.cratis.stage.extensions.protocol.TelemetryResponse
import io.grpc.Metadata
import io.grpc.ServerCall
import io.grpc.ServerCallHandler
import io.grpc.ServerInterceptor
import io.grpc.ServerInterceptors
import io.grpc.netty.shaded.io.grpc.netty.NettyChannelBuilder
import io.grpc.netty.shaded.io.grpc.netty.NettyServerBuilder
import java.io.File
import java.net.InetSocketAddress
import java.util.concurrent.CopyOnWriteArrayList
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue
import kotlinx.coroutines.delay
import kotlinx.coroutines.runBlocking

/**
 * The reference extension of the conformance suite, written with the Kotlin SDK. Every SDK implements the same behavior;
 * see Source/Extensions/Conformance/README.md.
 */
private fun conformanceExtension(): Extension = Extension.create("conformance-extension", "1.0.0")
    .setting("greeting", required = true)
    .secret("apiKey", required = true)
    .trigger("Notified")
    .ability("Echo", EffectClass.Safe) { call ->
        call.host.log(Level.Information, "echoing", mapOf("size" to call.input.toByteArray().size.toString()))
        call.host.counter("echo.calls")
        AbilityOutcome.Returned(call.input)
    }
    .ability("Greet", EffectClass.Idempotent, outcomes = listOf("greeted")) { call ->
        val greeting = call.host.setting("greeting")
        val key = call.host.secret("apiKey")
        val output = JsonObject()
        output.addProperty("greeting", greeting)
        output.addProperty("secretLength", key?.length)
        AbilityOutcome.Returned(output.toString(), "greeted")
    }
    .ability("Context", EffectClass.Safe) { call ->
        val output = JsonObject()
        output.addProperty("tenant", call.tenant)
        output.addProperty("system", call.system)
        output.addProperty("invocationId", call.invocationId)
        output.addProperty("idempotencyKey", call.idempotencyKey)
        output.addProperty("attempt", call.attempt)
        output.addProperty("causation", call.causation)
        output.addProperty("traceparent", call.traceparent)
        AbilityOutcome.Returned(output.toString())
    }
    .ability("Notify", EffectClass.Once) { call ->
        call.host.trigger("Notified", call.input)
        AbilityOutcome.Returned()
    }
    .ability("Defer", EffectClass.Once, outcomes = listOf("done")) { call ->
        // Accept now, answer later: the answer is given after the attempt has been acknowledged.
        Thread {
            Thread.sleep(50)
            runBlocking { call.host.answer("done", "{\"ok\":true}") }
        }.start()
        AbilityOutcome.Accepted
    }
    .ability("Refuse", EffectClass.Idempotent) { AbilityOutcome.NotSent("refused by design") }
    .ability("Reject", EffectClass.Once) { AbilityOutcome.Failed("rejected by the system", effectRuledOut = true) }
    .ability("Crash", EffectClass.Once) { error("boom") }
    .http("/hook") { call ->
        if (call.path == "/hook/events") {
            HttpReply(202, "received:".toByteArray() + call.body, mapOf("X-Echo" to listOf(call.query)))
        } else {
            HttpReply(404)
        }
    }

private class RecordedCall(val rpc: String, val request: Message, val authorization: String?)

/** Stage's side of the protocol as the conformance suite fakes it: it records every call with the credential it carried. */
private class FakeHost(
    private val settings: Map<String, String>,
    private val secrets: Map<String, String>
) : StageHostGrpcKt.StageHostCoroutineImplBase() {
    val calls = CopyOnWriteArrayList<RecordedCall>()

    private fun record(rpc: String, request: Message, credential: String?) {
        calls.add(RecordedCall(rpc, request, credential))
    }

    lateinit var credentialOf: () -> String?

    override suspend fun getSetting(request: GetSettingRequest): GetSettingResponse {
        record("GetSetting", request, credentialOf())
        val value = settings[request.name]
        return GetSettingResponse.newBuilder()
            .setStatus(if (value == null) GetSettingResponse.Status.STATUS_NOT_SET else GetSettingResponse.Status.STATUS_FOUND)
            .setValue(value ?: "")
            .build()
    }

    override suspend fun getSecret(request: GetSecretRequest): GetSecretResponse {
        record("GetSecret", request, credentialOf())
        val value = secrets[request.name]
        return GetSecretResponse.newBuilder()
            .setStatus(if (value == null) GetSecretResponse.Status.STATUS_NOT_SET else GetSecretResponse.Status.STATUS_FOUND)
            .setValue(value ?: "")
            .build()
    }

    override suspend fun raiseTrigger(request: RaiseTriggerRequest): RaiseTriggerResponse {
        record("RaiseTrigger", request, credentialOf())
        return RaiseTriggerResponse.newBuilder().setStatus(RaiseTriggerResponse.Status.STATUS_RAISED).build()
    }

    override suspend fun answer(request: AnswerRequest): AnswerResponse {
        record("Answer", request, credentialOf())
        return AnswerResponse.newBuilder().setStatus(AnswerResponse.Status.STATUS_ANSWERED).build()
    }

    override suspend fun log(request: LogRequest): TelemetryResponse {
        record("Log", request, credentialOf())
        return TelemetryResponse.getDefaultInstance()
    }

    override suspend fun recordMetrics(request: MetricsRequest): TelemetryResponse {
        record("RecordMetrics", request, credentialOf())
        return TelemetryResponse.getDefaultInstance()
    }
}

class ConformanceTest {
    @Test
    fun `the Kotlin SDK passes the conformance suite`() = runBlocking {
        val document = JsonParser.parseString(File(System.getProperty("conformance.cases")).readText()).asJsonObject
        val token = document["token"].asString
        val settings = document["host"].asJsonObject["settings"].asJsonObject.entrySet().associate { it.key to it.value.asString }
        val secrets = document["host"].asJsonObject["secrets"].asJsonObject.entrySet().associate { it.key to it.value.asString }

        // The credential each host call carried is read from its metadata by an interceptor and handed over in the call's context.
        val credentialKey = io.grpc.Context.key<String?>("credential")
        val fake = FakeHost(settings, secrets)
        fake.credentialOf = { credentialKey.get() }
        val interceptor = object : ServerInterceptor {
            override fun <ReqT, RespT> interceptCall(
                call: ServerCall<ReqT, RespT>,
                headers: Metadata,
                next: ServerCallHandler<ReqT, RespT>
            ): ServerCall.Listener<ReqT> {
                val value = headers.get(Metadata.Key.of("authorization", Metadata.ASCII_STRING_MARSHALLER))
                return io.grpc.Contexts.interceptCall(io.grpc.Context.current().withValue(credentialKey, value), call, headers, next)
            }
        }
        val hostServer = NettyServerBuilder.forAddress(InetSocketAddress("127.0.0.1", 0))
            .addService(ServerInterceptors.intercept(fake, interceptor))
            .build().start()
        val sdkServer = conformanceExtension().listen(0, "127.0.0.1").start()
        val channel = NettyChannelBuilder.forAddress("127.0.0.1", sdkServer.port).usePlaintext().build()
        val client = ExtensionBackendGrpcKt.ExtensionBackendCoroutineStub(channel)
        val parser = JsonFormat.parser()
        val printer = JsonFormat.printer().omittingInsignificantWhitespace()

        val failures = mutableListOf<String>()
        try {
            val cases = document["cases"].asJsonArray
            assertTrue(cases.size() > 10, "expected the conformance cases")
            for (element in cases) {
                val case = element.asJsonObject
                val name = case["name"].asString
                fake.calls.clear()
                val request = case["request"].toString().replace("@host", "http://127.0.0.1:${hostServer.port}")
                val response: Message = try {
                    when (case["rpc"].asString) {
                        "Describe" -> client.describe(DescribeRequest.newBuilder().also { parser.merge(request, it) }.build())
                        "Invoke" -> client.invoke(InvokeRequest.newBuilder().also { parser.merge(request, it) }.build())
                        "HandleHttp" -> client.handleHttp(InboundHttpRequest.newBuilder().also { parser.merge(request, it) }.build())
                        "Health" -> client.health(HealthRequest.getDefaultInstance())
                        else -> error("Unknown rpc ${case["rpc"]}.")
                    }
                } catch (error: Exception) {
                    failures.add("$name: $error")
                    continue
                }
                subset(case["expect"], JsonParser.parseString(printer.print(response)), "response", failures, name)

                val expectedCalls: JsonArray? = case["hostCalls"]?.asJsonArray
                if (expectedCalls != null) {
                    val deadline = System.currentTimeMillis() + 5000
                    while (fake.calls.size < expectedCalls.size() && System.currentTimeMillis() < deadline) delay(20)
                    val calls = fake.calls.toList()
                    if (calls.size != expectedCalls.size()) {
                        failures.add("$name: expected ${expectedCalls.size()} host calls, saw ${calls.size}.")
                        continue
                    }
                    expectedCalls.forEachIndexed { index, expected ->
                        val expectedCall = expected.asJsonObject
                        if (calls[index].rpc != expectedCall["rpc"].asString) failures.add("$name: host call $index was ${calls[index].rpc}, expected ${expectedCall["rpc"].asString}.")
                        if (calls[index].authorization != "Bearer $token") failures.add("$name: host call $index did not present the session credential.")
                        subset(expectedCall["request"], JsonParser.parseString(printer.print(calls[index].request)), "host call $index", failures, name)
                    }
                }
            }
        } finally {
            channel.shutdownNow()
            sdkServer.shutdownNow()
            hostServer.shutdownNow()
        }
        assertEquals(emptyList<String>(), failures, "the Kotlin SDK failed ${failures.size} conformance checks")
    }

    /** Everything expected is in actual: objects by key, arrays by index and length, scalars by value, and fields named *Json as JSON. */
    private fun subset(expected: JsonElement?, actual: JsonElement?, path: String, failures: MutableList<String>, name: String) {
        when {
            expected == null || expected.isJsonNull -> Unit
            expected.isJsonObject -> {
                if (actual == null || !actual.isJsonObject) {
                    failures.add("$name: $path is not an object.")
                    return
                }
                for ((key, value) in expected.asJsonObject.entrySet()) subset(value, actual.asJsonObject[key], "$path.$key", failures, name)
            }
            expected.isJsonArray -> {
                if (actual == null || !actual.isJsonArray || actual.asJsonArray.size() != expected.asJsonArray.size()) {
                    failures.add("$name: $path does not have ${expected.asJsonArray.size()} items.")
                    return
                }
                expected.asJsonArray.forEachIndexed { index, item -> subset(item, actual.asJsonArray[index], "$path[$index]", failures, name) }
            }
            else -> {
                var wanted = expected
                var got = actual
                if (path.endsWith("Json") && expected.isJsonPrimitive && expected.asJsonPrimitive.isString && actual != null && actual.isJsonPrimitive) {
                    wanted = JsonParser.parseString(expected.asString)
                    got = JsonParser.parseString(actual.asString)
                }
                if (wanted != got) failures.add("$name: $path was $got, expected $wanted.")
            }
        }
    }
}
