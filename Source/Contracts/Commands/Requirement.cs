// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents a <c>require</c> rule on a command — a condition the command as a whole must satisfy, rather than a
/// rule about one of its properties.
/// </summary>
/// <param name="Condition">The condition that must hold.</param>
/// <param name="Message">The message reported when it does not, or <see langword="null"/> when none is declared.</param>
/// <remarks>
/// Carries the same <see cref="ProducedEventCondition"/> tree a <c>produces when</c> clause carries, so <c>and</c>
/// and <c>or</c> mean here exactly what they mean there. A property rule says something about one value; a
/// requirement says something about the command as a whole — most often against state it
/// <see cref="ReadsDefinition">reads</see>.
/// </remarks>
public record Requirement(ProducedEventCondition Condition, string? Message);
