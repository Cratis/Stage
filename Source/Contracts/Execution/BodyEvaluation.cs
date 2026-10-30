// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>A single body invocation with tagged portable member values.</summary>
/// <param name="RequirementId">The implementation requirement being invoked.</param>
/// <param name="Members">The tagged portable member values, by name.</param>
public sealed record BodyEvaluationRequest(string RequirementId, ImmutableDictionary<string, JsonElement> Members);

/// <summary>The bounded worker verdict; unsupported outcomes are never validation rejections.</summary>
/// <param name="Outcome">The verdict kind.</param>
/// <param name="Value">The returned value, when the outcome carries one.</param>
/// <param name="Messages">The validation messages, when the outcome carries any.</param>
/// <param name="Diagnostic">The sanitized diagnostic line for a non-successful outcome.</param>
public sealed record BodyEvaluation(string Outcome, JsonElement? Value = null, ImmutableArray<string> Messages = default, string? Diagnostic = null)
{
    /// <summary>Gets a serializable, initialized collection even for non-validation outcomes or a <see langword="with"/> that clears it.</summary>
    public ImmutableArray<string> Messages { get; init => field = value.IsDefault ? [] : value; } = Messages.IsDefault ? [] : Messages;
}
