// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Collects conversion omissions without deciding whether a caller must refuse the result.
/// </summary>
public sealed class ConversionDiagnostics
{
    readonly List<ConversionDiagnostic> _all = [];

    /// <summary>
    /// Gets the diagnostics in reporting order through an append-only view.
    /// </summary>
    public IReadOnlyList<ConversionDiagnostic> All => _all.AsReadOnly();

    /// <summary>
    /// Gets whether any construct could not be converted faithfully.
    /// </summary>
    public bool HasErrors => _all.Exists(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Appends a diagnostic.
    /// </summary>
    /// <param name="diagnostic">The omission to record.</param>
    public void Report(ConversionDiagnostic diagnostic) => _all.Add(diagnostic);
}
