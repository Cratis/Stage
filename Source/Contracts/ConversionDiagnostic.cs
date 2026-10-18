// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Describes model content that could not be faithfully converted into a Stage contract.
/// </summary>
/// <param name="Code">The stable diagnostic code.</param>
/// <param name="Severity">Whether the omission prevents execution.</param>
/// <param name="Construct">The unsupported construct.</param>
/// <param name="Path">The owning slice or Scene structure.</param>
/// <param name="Location">The source location of the construct.</param>
/// <param name="Message">The explanation of the omission.</param>
public sealed record ConversionDiagnostic(string Code, DiagnosticSeverity Severity, string Construct, string Path, SourceLocation Location, string Message)
{
    /// <summary>
    /// Converts this diagnostic into Screenplay's diagnostic representation.
    /// </summary>
    /// <returns>The diagnostic suitable for a Screenplay caller.</returns>
    public Diagnostic ToDiagnostic() => new(Severity, Code, Message, Location);
}
