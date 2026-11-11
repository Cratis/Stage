// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.stage.extensions

import com.google.protobuf.Timestamp
import io.cratis.stage.extensions.protocol.AnswerRequest
import io.cratis.stage.extensions.protocol.GetSecretRequest
import io.cratis.stage.extensions.protocol.GetSecretResponse
import io.cratis.stage.extensions.protocol.GetSettingRequest
import io.cratis.stage.extensions.protocol.GetSettingResponse
import io.cratis.stage.extensions.protocol.KeyValue
import io.cratis.stage.extensions.protocol.LogLevel
import io.cratis.stage.extensions.protocol.LogRecord
import io.cratis.stage.extensions.protocol.LogRequest
import io.cratis.stage.extensions.protocol.Metric
import io.cratis.stage.extensions.protocol.MetricsRequest
import io.cratis.stage.extensions.protocol.RaiseTriggerRequest
import io.cratis.stage.extensions.protocol.RequestContext
import io.cratis.stage.extensions.protocol.StageHostGrpcKt
import io.grpc.Metadata
import io.grpc.stub.MetadataUtils
import java.time.Instant

/** A host service refused what the extension asked for, or was used before the handshake. */
public class HostServiceException(message: String) : Exception(message)

/** What came of raising a trigger. */
public data class TriggerResult(
    /** `Raised`, `UnknownTrigger`, `NotRouted` (nothing in the model reacts to it) or `Refused`. */
    val status: String,
    val detail: String
) {
    /** Whether the trigger was raised. */
    public val raised: Boolean get() = status == "Raised"
}

/** The level of a log record. */
public enum class Level(internal val protocol: LogLevel) {
    Trace(LogLevel.LOG_LEVEL_TRACE),
    Debug(LogLevel.LOG_LEVEL_DEBUG),
    Information(LogLevel.LOG_LEVEL_INFORMATION),
    Warning(LogLevel.LOG_LEVEL_WARNING),
    Error(LogLevel.LOG_LEVEL_ERROR),
    Critical(LogLevel.LOG_LEVEL_CRITICAL)
}

/** The connection the host services of one handshake are reached through. */
internal class HostConnection(val client: StageHostGrpcKt.StageHostCoroutineStub?, val credential: String)

/**
 * What Stage offers an extension: settings and secrets, triggers, answering later, and logs and metrics tied to the
 * invocation they are used from.
 */
public class HostServices internal constructor(private val connection: HostConnection, private val context: RequestContext) {
    /** Reads a public setting the extension declared. Returns null when it is not set. */
    public suspend fun setting(name: String): String? {
        val response = client().getSetting(GetSettingRequest.newBuilder().setContext(context).setName(name).build())
        return when (response.status) {
            GetSettingResponse.Status.STATUS_FOUND -> response.value
            GetSettingResponse.Status.STATUS_NOT_SET -> null
            GetSettingResponse.Status.STATUS_IS_SECRET -> throw HostServiceException("$name is declared as a secret: read it with secret().")
            else -> throw HostServiceException("$name is not a setting the extension declared.")
        }
    }

    /** Reads a secret the extension declared. Do not log it or return it from an ability. Returns null when it is not set. */
    public suspend fun secret(name: String): String? {
        val response = client().getSecret(GetSecretRequest.newBuilder().setContext(context).setName(name).build())
        return when (response.status) {
            GetSecretResponse.Status.STATUS_FOUND -> response.value
            GetSecretResponse.Status.STATUS_NOT_SET -> null
            GetSecretResponse.Status.STATUS_NOT_SECRET -> throw HostServiceException("$name is declared as a public setting: read it with setting().")
            else -> throw HostServiceException("$name is not a secret the extension declared.")
        }
    }

    /**
     * Raises a trigger the extension declared. The deduplication key makes raising it again find the first raise; within an
     * invocation it defaults to the idempotency key, outside one it is required.
     */
    public suspend fun trigger(trigger: String, payloadJson: String = "{}", deduplicationKey: String? = null): TriggerResult {
        val key = deduplicationKey ?: context.idempotencyKey.ifEmpty { null }
            ?: throw IllegalArgumentException("A trigger raised outside an invocation needs a deduplication key.")
        val response = client().raiseTrigger(
            RaiseTriggerRequest.newBuilder().setContext(context).setTrigger(trigger).setDeduplicationKey(key).setPayloadJson(payloadJson).build()
        )
        return TriggerResult(pascal(response.status.name), response.detail)
    }

    /**
     * Completes an invocation the extension accepted to answer later. The outlet is `returned` or a declared outcome. It
     * defaults to the invocation this was obtained for. Returns the status of the answer, `Answered` when it was taken.
     */
    public suspend fun answer(outlet: String, payloadJson: String = "{}", invocationId: String? = null): String {
        val target = if (invocationId == null) context else context.toBuilder().setInvocationId(invocationId).build()
        val response = client().answer(AnswerRequest.newBuilder().setContext(target).setOutlet(outlet).setPayloadJson(payloadJson).build())
        return pascal(response.status.name)
    }

    /** Writes a log record tied to the invocation. Never put a secret in it. */
    public suspend fun log(level: Level, message: String, attributes: Map<String, String> = emptyMap()) {
        val now = Instant.now()
        val record = LogRecord.newBuilder()
            .setLevel(level.protocol)
            .setMessage(message)
            .setOccurred(Timestamp.newBuilder().setSeconds(now.epochSecond).setNanos(now.nano))
            .addAllAttributes(pairs(attributes))
            .build()
        client().log(LogRequest.newBuilder().setContext(context).addRecords(record).build())
    }

    /** Adds to a counter tied to the invocation. */
    public suspend fun counter(name: String, value: Double = 1.0, attributes: Map<String, String> = emptyMap()) {
        record(Metric.newBuilder().setName(name).setKind(Metric.Kind.KIND_COUNTER).setValue(value).addAllAttributes(pairs(attributes)).build())
    }

    /** Records one measurement of a distribution, in a unit such as `s`. */
    public suspend fun histogram(name: String, value: Double, unit: String = "", attributes: Map<String, String> = emptyMap()) {
        record(Metric.newBuilder().setName(name).setKind(Metric.Kind.KIND_HISTOGRAM).setValue(value).setUnit(unit).addAllAttributes(pairs(attributes)).build())
    }

    private suspend fun record(metric: Metric) {
        client().recordMetrics(MetricsRequest.newBuilder().setContext(context).addMetrics(metric).build())
    }

    private fun client(): StageHostGrpcKt.StageHostCoroutineStub {
        val stub = connection.client
            ?: throw HostServiceException("Stage has not completed the handshake with this extension, so the host services are not reachable yet.")
        val headers = Metadata()
        headers.put(Metadata.Key.of("authorization", Metadata.ASCII_STRING_MARSHALLER), "Bearer ${connection.credential}")
        return stub.withInterceptors(MetadataUtils.newAttachHeadersInterceptor(headers))
    }

    private fun pairs(attributes: Map<String, String>): List<KeyValue> =
        attributes.map { (key, value) -> KeyValue.newBuilder().setKey(key).setValue(value).build() }

    /** Turns STATUS_UNKNOWN_TRIGGER into UnknownTrigger. */
    private fun pascal(status: String): String =
        status.removePrefix("STATUS_").lowercase().split('_').joinToString("") { part -> part.replaceFirstChar { it.uppercase() } }
}
