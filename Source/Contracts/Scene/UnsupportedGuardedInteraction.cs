// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when a guarded interaction has a guard the Stage runtime cannot evaluate.
/// </summary>
/// <param name="interaction">The authored interaction.</param>
/// <param name="location">The interaction's source location.</param>
public sealed class UnsupportedGuardedInteraction(string interaction, SourceLocation location) : Exception(
    $"{DiagnosticCode}: Guarded interaction '{interaction}' at {location} has a guard the Stage runtime cannot evaluate. " +
    "Stage evaluates guards that compare 'item.<field>' with a literal, combined with 'and' and 'or'. " +
    "No runnable application or live scene was produced. See https://github.com/Cratis/Scene/issues/68 and https://github.com/Cratis/Stage/issues/209.")
{
    /// <summary>
    /// The stable diagnostic code for a guarded interaction the Stage runtime cannot evaluate.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-INTERACTION-001";
}
