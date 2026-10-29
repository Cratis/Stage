// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>A single body invocation with tagged portable member values.</summary>
public sealed record BodyEvaluationRequest(string RequirementId, ImmutableDictionary<string, JsonElement> Members);

/// <summary>The bounded worker verdict; unsupported outcomes are never validation rejections.</summary>
public sealed record BodyEvaluation(string Outcome, JsonElement? Value = null, ImmutableArray<string> Messages = default, string? Diagnostic = null)
{
    /// <summary>Gets a serializable, initialized collection even for non-validation outcomes or a <c>with</c> that clears it.</summary>
    public ImmutableArray<string> Messages { get; init => field = value.IsDefault ? [] : value; } = Messages.IsDefault ? [] : Messages;
}
