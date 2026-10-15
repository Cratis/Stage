// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a legacy renderer cannot preserve a validation rule.
/// </summary>
/// <param name="rule">The unsupported authored validation rule.</param>
public sealed class UnsupportedValidationRule(ValidationRuleSyntax rule) : Exception(
    $"{DiagnosticCode}: Validation rule '{rule.Rule}' on '{rule.Property}' at {rule.Location} cannot be preserved by Stage's legacy renderer.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported validation rules.
    /// </summary>
    public const string DiagnosticCode = SemanticVersionFeatures.UnsupportedValidation;

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = rule.Location;
}
