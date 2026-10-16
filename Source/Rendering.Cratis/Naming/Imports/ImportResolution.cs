// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents the using directives of a generated file and the spelling of every type it names.
/// </summary>
/// <param name="Usings">The namespaces to import inside the file-scoped namespace, sorted ordinally.</param>
/// <param name="Spellings">The spelling of each reference: its short name, or its fully qualified name when a short name cannot be proven to bind to it.</param>
internal sealed record ImportResolution(ImmutableArray<string> Usings, ImmutableDictionary<TypeReference, string> Spellings)
{
    /// <summary>
    /// Gets the existing extension-method imports, retained above the file-scoped namespace.
    /// </summary>
    public ImmutableArray<string> TopLevelUsings { get; init; } = [];

    /// <summary>
    /// Gets the spelling of a reference.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <returns>The spelling; the fully qualified name for a reference the request did not include.</returns>
    public string Spell(TypeReference reference) => Spellings.GetValueOrDefault(reference) ?? reference.Qualified;
}
