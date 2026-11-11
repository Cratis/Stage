// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package extension

import protocol "github.com/Cratis/Stage/Source/Extensions/Go/protocol/v1"

// EffectClass says how an ability behaves when it is performed more than once, which decides what Stage may do after
// an unknown outcome.
type EffectClass int

const (
	// Safe abilities change nothing when performed again. Stage retries freely.
	Safe EffectClass = iota
	// Idempotent abilities have the same effect performed again with the same idempotency key. Stage retries.
	Idempotent
	// Once abilities must take effect at most once. After an unknown outcome Stage stops for reconciliation.
	Once
)

func (e EffectClass) protocol() protocol.EffectClass {
	switch e {
	case Safe:
		return protocol.EffectClass_EFFECT_CLASS_SAFE
	case Idempotent:
		return protocol.EffectClass_EFFECT_CLASS_IDEMPOTENT
	default:
		return protocol.EffectClass_EFFECT_CLASS_ONCE
	}
}

// Outcome is what an ability answers for an attempt. Create one with Returned, Accepted, NotSent or Failed.
type Outcome struct {
	response *protocol.InvokeResponse
}

// Returned says the system did the work. output is JSON and defaults to an empty object; outcome names one of the
// outcomes the ability declares, or is empty for a plain return.
func Returned(output string, outcome string) Outcome {
	return Outcome{&protocol.InvokeResponse{Result: &protocol.InvokeResponse_Returned{Returned: &protocol.Returned{Outcome: outcome, OutputJson: output}}}}
}

// Accepted says the system took the work and will answer later, which the extension then does with Host.Answer.
func Accepted() Outcome {
	return Outcome{&protocol.InvokeResponse{Result: &protocol.InvokeResponse_Accepted{Accepted: &protocol.Accepted{}}}}
}

// NotSent says the call certainly did not reach the system, so nothing happened and it is safe to try again, even for
// a once ability.
func NotSent(reason string) Outcome {
	return Outcome{&protocol.InvokeResponse{Result: &protocol.InvokeResponse_NotSent{NotSent: &protocol.NotSent{Reason: reason}}}}
}

// Failed says the call failed. effectRuledOut is true only when the extension can rule out that the effect happened;
// leave it false when unsure, which is what an error or a timeout means.
func Failed(reason string, effectRuledOut bool) Outcome {
	return Outcome{&protocol.InvokeResponse{Result: &protocol.InvokeResponse_Failed{Failed: &protocol.Failed{Reason: reason, EffectRuledOut: effectRuledOut}}}}
}
