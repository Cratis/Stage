// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when a guarded screen action has a guard or fallback the Stage runtime cannot evaluate.
/// </summary>
/// <param name="label">The authored action label.</param>
/// <param name="location">The authored action's source location.</param>
public sealed class UnsupportedGuardedScreenAction(string label, SourceLocation location) : Exception(
    $"{DiagnosticCode}: Guarded screen action '{label}' at {location} has a guard the Stage runtime cannot evaluate. " +
    "Stage evaluates guards that compare 'item.<field>' with a literal, combined with 'and' and 'or', each alternative naming a command, " +
    "with a 'hidden' or executable 'otherwise'. No runnable application or live scene was produced. " +
    "See https://github.com/Cratis/Scene/issues/68 and https://github.com/Cratis/Stage/issues/209.")
{
    /// <summary>
    /// The stable diagnostic code for a guarded screen action the Stage runtime cannot evaluate.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-ACTION-001";

    /// <summary>
    /// Gets the authored action label.
    /// </summary>
    public string Label { get; } = label;

    /// <summary>
    /// Gets the authored action's source location, including its file path when known.
    /// </summary>
    public SourceLocation Location { get; } = location;
}
