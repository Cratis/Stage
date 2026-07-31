// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents the condition guarding a <see cref="ProducedEvent"/> — the modeled <c>produces when</c> clause.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ProducedEventComparison), "comparison")]
[JsonDerivedType(typeof(ProducedEventLogicalCondition), "logical")]
public abstract record ProducedEventCondition;

/// <summary>
/// Represents a comparison of a command property against a constant, such as <c>status == "sent"</c>.
/// </summary>
/// <param name="Property">The name of the command property being compared.</param>
/// <param name="Operator">The comparison to apply.</param>
/// <param name="Value">The constant to compare against, as JSON text.</param>
public record ProducedEventComparison(string Property, ProducedEventComparisonOperator Operator, string Value) : ProducedEventCondition;

/// <summary>
/// Represents two conditions combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand condition.</param>
/// <param name="Operator">The operator combining the conditions.</param>
/// <param name="Right">The right hand condition.</param>
public record ProducedEventLogicalCondition(ProducedEventCondition Left, ProducedEventLogicalOperator Operator, ProducedEventCondition Right) : ProducedEventCondition;
