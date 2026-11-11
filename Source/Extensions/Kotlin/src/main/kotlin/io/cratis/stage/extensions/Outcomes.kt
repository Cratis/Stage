// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.stage.extensions

/**
 * How an ability behaves when it is performed more than once, which decides what Stage may do after an unknown outcome.
 */
public enum class EffectClass {
    /** Performing it again changes nothing. Stage retries freely. */
    Safe,

    /** Performing it again with the same idempotency key has the same effect as once. Stage retries. */
    Idempotent,

    /** The effect must happen at most once. After an unknown outcome Stage stops for reconciliation. */
    Once
}

/** What an ability answers for an attempt. */
public sealed interface AbilityOutcome {
    /**
     * The system did the work.
     *
     * @property output What the ability returns, as JSON.
     * @property outcome The named outcome the ability declares, or empty for a plain return.
     */
    public data class Returned(val output: String = "{}", val outcome: String = "") : AbilityOutcome

    /** The system accepted the work and will answer later, which the extension then does with [HostServices.answer]. */
    public data object Accepted : AbilityOutcome

    /** The call certainly did not reach the system, so nothing happened and it is safe to try again, even for a once ability. */
    public data class NotSent(val reason: String) : AbilityOutcome

    /**
     * The call failed.
     *
     * @property reason What went wrong.
     * @property effectRuledOut True only when the extension can rule out that the effect happened. Leave it false when unsure,
     * which is what an exception or a timeout means.
     */
    public data class Failed(val reason: String, val effectRuledOut: Boolean = false) : AbilityOutcome
}

/** An inbound HTTP request Stage routed to the extension. Cookies and authorization are never forwarded. */
public class HttpCall(
    public val method: String,
    public val path: String,
    public val query: String,
    public val headers: Map<String, List<String>>,
    public val body: ByteArray
)

/** The answer to an [HttpCall]. */
public class HttpReply(
    public val status: Int,
    public val body: ByteArray = ByteArray(0),
    public val headers: Map<String, List<String>> = emptyMap()
)
