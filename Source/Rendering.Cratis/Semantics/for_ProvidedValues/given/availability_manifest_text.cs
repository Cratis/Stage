// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;

/// <summary>
/// Cuts the manifest statements out of a generated availability registration.
/// </summary>
internal static class availability_manifest_text
{
    /// <summary>
    /// Takes the text from the manifest declaration to the statement that registers it.
    /// </summary>
    /// <param name="registration">The generated registration.</param>
    /// <returns>The manifest statements, with line endings normalized.</returns>
    internal static string From(string registration)
    {
        var normalized = registration.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = normalized.IndexOf("var manifest = ", StringComparison.Ordinal);
        var end = normalized.IndexOf("InMemoryAvailabilityManifest(manifest));", start, StringComparison.Ordinal);
        return normalized[start..end];
    }
}
#endif
