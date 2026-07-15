// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Stage.Contracts.Rules;

/// <summary>
/// Represents a single validation rule applied to a command property.
/// </summary>
/// <remarks>
/// The rule hierarchy mirrors the closed vocabulary supported by the Cratis Arc validation engine on both the C#
/// (FluentValidation) and TypeScript sides. Each derived type carries a stable <c>kind</c> discriminator used for
/// polymorphic JSON serialization; the engine reconstructs a FluentValidation rule from each one.
/// </remarks>
/// <param name="ErrorMessage">The optional custom error message for the rule.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NotEmpty), "notEmpty")]
[JsonDerivedType(typeof(NotNull), "notNull")]
[JsonDerivedType(typeof(MinLength), "minLength")]
[JsonDerivedType(typeof(MaxLength), "maxLength")]
[JsonDerivedType(typeof(Length), "length")]
[JsonDerivedType(typeof(EmailAddress), "emailAddress")]
[JsonDerivedType(typeof(Phone), "phone")]
[JsonDerivedType(typeof(Url), "url")]
[JsonDerivedType(typeof(Matches), "matches")]
[JsonDerivedType(typeof(GreaterThan), "greaterThan")]
[JsonDerivedType(typeof(GreaterThanOrEqual), "greaterThanOrEqual")]
[JsonDerivedType(typeof(LessThan), "lessThan")]
[JsonDerivedType(typeof(LessThanOrEqual), "lessThanOrEqual")]
public abstract record RuleDefinition(string? ErrorMessage);
