// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents a language's verdict on one authored body.
/// </summary>
/// <param name="Outcome">The verdict.</param>
/// <param name="Code">The language-owned code; empty when admitted.</param>
/// <param name="Message">The reason; empty when admitted.</param>
public sealed record AuthoredAdmission(AuthoredAdmissionOutcome Outcome, string Code, string Message)
{
    /// <summary>
    /// Gets the verdict for a body the language accepts as it is.
    /// </summary>
    public static AuthoredAdmission Admitted { get; } = new(AuthoredAdmissionOutcome.Admitted, string.Empty, string.Empty);

    /// <summary>
    /// Creates the verdict for a body the language cannot support; the construct becomes a gap.
    /// </summary>
    /// <param name="code">The language-owned code.</param>
    /// <param name="message">The reason.</param>
    /// <returns>The verdict.</returns>
    public static AuthoredAdmission Unsupported(string code, string message) => new(AuthoredAdmissionOutcome.Unsupported, code, message);

    /// <summary>
    /// Creates the verdict for a body that fails an integrity or trust check; the plan is refused.
    /// </summary>
    /// <param name="code">The language-owned code.</param>
    /// <param name="message">The reason.</param>
    /// <returns>The verdict.</returns>
    public static AuthoredAdmission Refused(string code, string message) => new(AuthoredAdmissionOutcome.Refused, code, message);
}
