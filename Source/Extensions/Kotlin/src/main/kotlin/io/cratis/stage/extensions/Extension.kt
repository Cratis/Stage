// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.stage.extensions

import com.google.protobuf.ByteString
import io.cratis.stage.extensions.protocol.AbilityDeclaration
import io.cratis.stage.extensions.protocol.Accepted
import io.cratis.stage.extensions.protocol.DescribeRequest
import io.cratis.stage.extensions.protocol.DescribeResponse
import io.cratis.stage.extensions.protocol.ExtensionBackendGrpcKt
import io.cratis.stage.extensions.protocol.Failed
import io.cratis.stage.extensions.protocol.HealthRequest
import io.cratis.stage.extensions.protocol.HealthResponse
import io.cratis.stage.extensions.protocol.Header
import io.cratis.stage.extensions.protocol.InboundHttpRequest
import io.cratis.stage.extensions.protocol.InboundHttpResponse
import io.cratis.stage.extensions.protocol.InvokeRequest
import io.cratis.stage.extensions.protocol.InvokeResponse
import io.cratis.stage.extensions.protocol.NotSent
import io.cratis.stage.extensions.protocol.ProtocolVersion
import io.cratis.stage.extensions.protocol.Returned
import io.cratis.stage.extensions.protocol.SettingDeclaration
import io.cratis.stage.extensions.protocol.StageHostGrpcKt
import io.cratis.stage.extensions.protocol.TriggerDeclaration
import io.grpc.ManagedChannel
import io.grpc.Server
import io.grpc.Status
import io.grpc.StatusException
import io.grpc.netty.shaded.io.grpc.netty.NettyChannelBuilder
import io.grpc.netty.shaded.io.grpc.netty.NettyServerBuilder
import java.net.InetSocketAddress
import java.time.Instant
import kotlin.coroutines.cancellation.CancellationException

private const val ProtocolMajor = 1
private const val ProtocolMinor = 0

/** One attempt at an ability, as the handler receives it. */
public class AbilityContext internal constructor(
    /** The tenant the work runs for. */
    public val tenant: String,
    /** The system whose ability is performed. */
    public val system: String,
    /** The invocation identity. */
    public val invocationId: String,
    /** The key the effect is deduplicated on. It is the same for every attempt of the invocation. */
    public val idempotencyKey: String,
    /** The attempt number, starting at 1. */
    public val attempt: Int,
    /** When the attempt runs out of time, or null when no deadline was imposed. */
    public val deadline: Instant?,
    /** What caused the invocation. */
    public val causation: String,
    /** The W3C trace context, so the extension's own telemetry joins the invocation's trace. */
    public val traceparent: String,
    /** The ability's input, as JSON. */
    public val input: String,
    /** The host services, tied to this invocation. */
    public val host: HostServices
)

private class Ability(
    val name: String,
    val contractMajor: Int,
    val effect: EffectClass,
    val outcomes: List<String>,
    val handler: suspend (AbilityContext) -> AbilityOutcome
)

private class Setting(val name: String, val secret: Boolean, val required: Boolean)

private class Route(val prefix: String, val handler: suspend (HttpCall) -> HttpReply)

/**
 * Describes an extension backend: what it realizes and how. Declare it, then [listen] for Stage to connect.
 */
public class Extension private constructor(private val name: String, private val version: String) {
    private val abilities = mutableListOf<Ability>()
    private val triggers = mutableListOf<String>()
    private val settings = mutableListOf<Setting>()
    private val routes = mutableListOf<Route>()

    /**
     * Declares an ability the extension realizes. Stage checks it against the contract of the system at the handshake,
     * so [effect] and [outcomes] must be what the contract declares.
     */
    public fun ability(
        name: String,
        effect: EffectClass,
        outcomes: List<String> = emptyList(),
        contractMajor: Int = 1,
        handler: suspend (AbilityContext) -> AbilityOutcome
    ): Extension {
        abilities.removeAll { it.name == name }
        abilities.add(Ability(name, contractMajor, effect, outcomes, handler))
        return this
    }

    /** Declares a trigger the extension raises. */
    public fun trigger(name: String): Extension {
        triggers.add(name)
        return this
    }

    /** Declares a public setting the extension reads. */
    public fun setting(name: String, required: Boolean = false): Extension {
        settings.add(Setting(name, secret = false, required = required))
        return this
    }

    /** Declares a secret the extension reads. It is delivered by the host services and nowhere else. */
    public fun secret(name: String, required: Boolean = false): Extension {
        settings.add(Setting(name, secret = true, required = required))
        return this
    }

    /** Handles inbound HTTP requests below a path prefix, such as a webhook. */
    public fun http(prefix: String, handler: suspend (HttpCall) -> HttpReply): Extension {
        routes.add(Route(prefix, handler))
        return this
    }

    /** Creates a gRPC server that speaks the extension protocol for the extension, listening on [port] (0 chooses one). */
    public fun listen(port: Int, host: String = "0.0.0.0"): Server =
        NettyServerBuilder.forAddress(InetSocketAddress(host, port)).addService(Backend(this)).build()

    private class Backend(private val extension: Extension) : ExtensionBackendGrpcKt.ExtensionBackendCoroutineImplBase() {
        private var channel: ManagedChannel? = null
        private var connection = HostConnection(null, "")

        override suspend fun describe(request: DescribeRequest): DescribeResponse {
            if (request.protocolVersion.major != ProtocolMajor) {
                throw StatusException(
                    Status.FAILED_PRECONDITION.withDescription("This SDK speaks protocol major $ProtocolMajor; Stage speaks ${request.protocolVersion.major}.")
                )
            }

            // The credential arrives here and is kept in memory: nowhere else, never in the environment or a file.
            channel?.shutdownNow()
            channel = null
            var stub: StageHostGrpcKt.StageHostCoroutineStub? = null
            if (request.hostEndpoint.isNotEmpty()) {
                val target = request.hostEndpoint.removePrefix("http://").removePrefix("https://")
                channel = NettyChannelBuilder.forTarget(target).usePlaintext().build()
                stub = StageHostGrpcKt.StageHostCoroutineStub(channel!!)
            }
            connection = HostConnection(stub, request.sessionToken)

            val response = DescribeResponse.newBuilder()
                .setProtocolVersion(ProtocolVersion.newBuilder().setMajor(ProtocolMajor).setMinor(ProtocolMinor))
                .setName(extension.name)
                .setVersion(extension.version)
            extension.abilities.forEach { ability ->
                response.addAbilities(
                    AbilityDeclaration.newBuilder()
                        .setName(ability.name)
                        .setContractMajor(ability.contractMajor)
                        .setEffect(effect(ability.effect))
                        .addAllOutcomes(ability.outcomes)
                )
            }
            extension.triggers.forEach { response.addTriggers(TriggerDeclaration.newBuilder().setName(it)) }
            extension.settings.forEach {
                response.addSettings(SettingDeclaration.newBuilder().setName(it.name).setSecret(it.secret).setRequired(it.required))
            }
            extension.routes.forEach { response.addHttpRoutes(it.prefix) }
            return response.build()
        }

        override suspend fun invoke(request: InvokeRequest): InvokeResponse {
            val ability = extension.abilities.firstOrNull { it.name == request.ability }
            if (ability == null || ability.contractMajor != request.contractMajor) {
                return InvokeResponse.newBuilder()
                    .setNotSent(NotSent.newBuilder().setReason("The extension does not realize ${request.ability}@${request.contractMajor}."))
                    .build()
            }

            val context = request.context
            val call = AbilityContext(
                tenant = context.tenant,
                system = context.system,
                invocationId = context.invocationId,
                idempotencyKey = context.idempotencyKey,
                attempt = context.attempt,
                deadline = if (context.hasDeadline()) Instant.ofEpochSecond(context.deadline.seconds, context.deadline.nanos.toLong()) else null,
                causation = context.causation,
                traceparent = context.traceparent,
                input = request.inputJson,
                host = HostServices(connection, context)
            )
            return try {
                map(ability.handler(call))
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (error: Exception) {
                // An exception says nothing about whether the effect happened, so it is not ruled out.
                map(AbilityOutcome.Failed(error.message ?: error.toString()))
            }
        }

        override suspend fun handleHttp(request: InboundHttpRequest): InboundHttpResponse {
            val route = extension.routes
                .filter { request.path == it.prefix || request.path.startsWith(it.prefix.trimEnd('/') + "/") }
                .maxByOrNull { it.prefix.length }
                ?: return InboundHttpResponse.newBuilder().setStatus(404).build()

            val headers = request.headersList.associate { it.name to it.valuesList.toList() }
            return try {
                val reply = route.handler(HttpCall(request.method, request.path, request.query, headers, request.body.toByteArray()))
                val response = InboundHttpResponse.newBuilder().setStatus(reply.status).setBody(ByteString.copyFrom(reply.body))
                reply.headers.forEach { (name, values) -> response.addHeaders(Header.newBuilder().setName(name).addAllValues(values)) }
                response.build()
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (_: Exception) {
                InboundHttpResponse.newBuilder().setStatus(500).build()
            }
        }

        override suspend fun health(request: HealthRequest): HealthResponse =
            HealthResponse.newBuilder().setStatus(HealthResponse.Status.STATUS_SERVING).build()

        private fun map(outcome: AbilityOutcome): InvokeResponse = when (outcome) {
            is AbilityOutcome.Returned -> InvokeResponse.newBuilder()
                .setReturned(Returned.newBuilder().setOutcome(outcome.outcome).setOutputJson(outcome.output))
                .build()
            AbilityOutcome.Accepted -> InvokeResponse.newBuilder().setAccepted(Accepted.getDefaultInstance()).build()
            is AbilityOutcome.NotSent -> InvokeResponse.newBuilder().setNotSent(NotSent.newBuilder().setReason(outcome.reason)).build()
            is AbilityOutcome.Failed -> InvokeResponse.newBuilder()
                .setFailed(Failed.newBuilder().setReason(outcome.reason).setEffectRuledOut(outcome.effectRuledOut))
                .build()
        }

        private fun effect(effect: EffectClass): io.cratis.stage.extensions.protocol.EffectClass = when (effect) {
            EffectClass.Safe -> io.cratis.stage.extensions.protocol.EffectClass.EFFECT_CLASS_SAFE
            EffectClass.Idempotent -> io.cratis.stage.extensions.protocol.EffectClass.EFFECT_CLASS_IDEMPOTENT
            EffectClass.Once -> io.cratis.stage.extensions.protocol.EffectClass.EFFECT_CLASS_ONCE
        }
    }

    public companion object {
        /** Starts describing an extension. */
        public fun create(name: String, version: String): Extension = Extension(name, version)
    }
}
