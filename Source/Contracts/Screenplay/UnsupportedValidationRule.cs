// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// The exception that is thrown when an unknown validation rule would be dropped by the default engine.
/// </summary>
/// <param name="rule">The unsupported authored rule.</param>
public sealed class UnsupportedValidationRule(ValidationRuleSyntax rule) : Exception(
    $"{DiagnosticCode}: Validation rule kind '{rule.Rule}' on property '{rule.Property}' at {rule.Location} cannot be preserved by Stage's default engine.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported validation rules.
    /// </summary>
    public const string DiagnosticCode = "STAGE-ESM-005";

    /// <summary>
    /// Gets the authored source location.
    /// </summary>
    public SourceLocation Location { get; } = rule.Location;
}
