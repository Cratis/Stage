// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// The exception that is thrown when a legacy model-bound projection cannot preserve a declaration.
/// </summary>
/// <param name="projection">The authored projection name.</param>
/// <param name="construct">The construct that cannot be expressed.</param>
public sealed class UnsupportedLegacyProjection(string projection, string construct)
    : Exception($"{DiagnosticCode}: Projection '{projection}' cannot render '{construct}' with legacy model-bound attributes. No affected slice artifact was emitted.")
{
    /// <summary>
    /// The stable diagnostic for unsupported legacy projection semantics.
    /// </summary>
    public const string DiagnosticCode = "STAGE-CRATIS-PROJECTION-001";
}
