// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Provides the authoritative integrity check for a complete emitted Java file, using one JLS 3.3 translation of the whole text.
/// </summary>
/// <remarks>
/// The S6 resolver MUST call <see cref="Validate"/> on every complete resolved file before returning output.
/// This complete-file check is the integrity guarantee. Segment pre-validation is only conservative defense in depth;
/// it cannot replace this check or establish the integrity of resolved output.
/// </remarks>
public static class JavaOutputIntegrity
{
    /// <summary>
    /// Checks raw and once-decoded complete output for private-use scalar values and lone UTF-16 surrogates.
    /// </summary>
    /// <param name="text">The complete emitted Java file, after all segments have been resolved and joined.</param>
    /// <exception cref="LanguagePackRefusal">
    /// STAGE-JAVA-0002 for malformed eligible Unicode escapes, STAGE-JAVA-0003 for raw or decoded private-use values,
    /// or STAGE-JAVA-0004 for raw or decoded lone surrogates, as catalogued by <see cref="JavaRefusalCodes"/>.
    /// </exception>
    public static void Validate(string text) => PrivateUseCodePoints.Validate(text);
}
