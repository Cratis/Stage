// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a legacy renderer cannot preserve a validation rule.
/// </summary>
public sealed class UnsupportedValidationRule : Exception
{
    /// <summary>
    /// The stable diagnostic code for unsupported validation rules.
    /// </summary>
    public const string DiagnosticCode = SemanticVersionFeatures.UnsupportedValidation;

    /// <summary>
    /// Initializes a refusal for an authored validation rule that escaped admission.
    /// </summary>
    /// <param name="rule">The unsupported authored validation rule.</param>
    public UnsupportedValidationRule(ValidationRuleSyntax rule)
        : base($"{Code(rule.Rule)}: Validation rule '{rule.Rule}' on '{rule.Property}' at {rule.Location} cannot be preserved by Stage's legacy renderer.")
    {
        Location = rule.Location;
    }

    /// <summary>
    /// Initializes an internal-contract refusal for an unmapped rule kind that escaped admission.
    /// </summary>
    /// <param name="kind">The unsupported rule kind.</param>
    public UnsupportedValidationRule(ValidationRuleKind kind)
        : base($"{Code(kind)}: Validation rule kind '{kind}' escaped admission and has no legacy renderer mapping.")
    {
        Location = SourceLocation.Start;
    }

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; }

    static string Code(ValidationRuleKind kind) => Enum.IsDefined(kind) ? DiagnosticCode : "STAGE-ESM-005";
}
