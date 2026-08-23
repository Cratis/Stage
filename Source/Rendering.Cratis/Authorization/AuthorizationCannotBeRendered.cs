// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// The exception that is thrown when a Screenplay authorization requirement cannot be rendered faithfully.
/// </summary>
/// <param name="subject">The artifact whose authorization cannot be rendered.</param>
/// <param name="reason">Why the authorization cannot be rendered.</param>
public class AuthorizationCannotBeRendered(string subject, string reason) : Exception(
    $"{DiagnosticCode}: {subject} {reason}. The artifact was not rendered because faithful authorization requires " +
    "the future Screenplay-owned portable policy backend.")
{
    /// <summary>
    /// The stable diagnostic code for unsupported authorization rendering.
    /// </summary>
    public const string DiagnosticCode = "STAGE-AUTH-001";
}
