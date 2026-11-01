// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Defines how a language judged an authored body.
/// </summary>
public enum AuthoredAdmissionOutcome
{
    /// <summary>
    /// The body is kept as it is.
    /// </summary>
    Admitted,

    /// <summary>
    /// The language cannot support the body; the construct is degraded with a gap.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The body is unsafe; the whole plan is refused.
    /// </summary>
    Refused
}
