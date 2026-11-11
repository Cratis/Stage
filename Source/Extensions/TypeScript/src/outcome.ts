// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * How an ability behaves when it is performed more than once, which decides what Stage may do after an unknown outcome:
 * `safe` changes nothing, `idempotent` has the same effect with the same idempotency key, `once` must take effect at
 * most once, so Stage stops for reconciliation instead of retrying.
 */
export type EffectClass = 'safe' | 'idempotent' | 'once';

/** What an ability answers for an attempt. Create one with {@link outcome}. */
export type AbilityOutcome =
    | { kind: 'returned'; output: string; outcome: string }
    | { kind: 'accepted' }
    | { kind: 'notSent'; reason: string }
    | { kind: 'failed'; reason: string; effectRuledOut: boolean };

/** Creates the outcomes an ability can answer with. */
export const outcome = {
    /** The system did the work. `output` is JSON; `named` is one of the outcomes the ability declares. */
    returned: (output = '{}', named = ''): AbilityOutcome => ({ kind: 'returned', output, outcome: named }),
    /** The system accepted the work and will answer later, which the extension then does with `host.answer`. */
    accepted: (): AbilityOutcome => ({ kind: 'accepted' }),
    /** The call certainly did not reach the system, so nothing happened and it is safe to try again, even for a once ability. */
    notSent: (reason: string): AbilityOutcome => ({ kind: 'notSent', reason }),
    /** The call failed. `effectRuledOut` is true only when the extension can rule out that the effect happened. */
    failed: (reason: string, effectRuledOut = false): AbilityOutcome => ({ kind: 'failed', reason, effectRuledOut })
};

/** @internal */
export function toInvokeResponse(result: AbilityOutcome): object {
    switch (result.kind) {
        case 'returned': return { returned: { outcome: result.outcome, outputJson: result.output } };
        case 'accepted': return { accepted: {} };
        case 'notSent': return { notSent: { reason: result.reason } };
        case 'failed': return { failed: { reason: result.reason, effectRuledOut: result.effectRuledOut } };
    }
}

/** @internal */
export const effectNames: Record<EffectClass, string> = {
    safe: 'EFFECT_CLASS_SAFE',
    idempotent: 'EFFECT_CLASS_IDEMPOTENT',
    once: 'EFFECT_CLASS_ONCE'
};
