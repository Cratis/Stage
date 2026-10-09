// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception thrown when guarded interaction alternatives cannot run faithfully in the pinned Scene packages.
/// </summary>
/// <param name="interaction">The authored interaction.</param>
/// <param name="location">The interaction's source location.</param>
public sealed class UnsupportedGuardedInteraction(string interaction, SourceLocation location) : Exception(
    $"{DiagnosticCode}: Guarded interaction '{interaction}' at {location} cannot run in the pinned Scene packages. " +
    "Scene needs ordered first-match action lists, fallback actions, and no execution without a subject. " +
    "No runnable application or live scene was produced. See https://github.com/Cratis/Scene/issues/68 and https://github.com/Cratis/Stage/issues/209.")
{
    /// <summary>
    /// The stable diagnostic code for guarded interactions requiring Scene runtime support.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-INTERACTION-001";
}
