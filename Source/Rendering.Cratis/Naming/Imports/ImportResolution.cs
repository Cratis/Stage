// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents the using directives of a generated file and the spelling of every type it names.
/// </summary>
/// <param name="Usings">The namespaces to import with directives inside the file-scoped namespace, where a directive above it could not be proven to bind the same; sorted ordinally and usually empty.</param>
/// <param name="Spellings">The spelling of each reference: its short name, or its fully qualified name when a short name cannot be proven to bind to it.</param>
internal sealed record ImportResolution(ImmutableArray<string> Usings, ImmutableDictionary<TypeReference, string> Spellings)
{
    /// <summary>
    /// Gets the namespaces to import with <c language="csharp">using</c> directives above the file-scoped namespace, sorted
    /// ordinally. A namespace a global using already imports is never listed.
    /// </summary>
    public ImmutableArray<string> TopLevelUsings { get; init; } = [];

    /// <summary>
    /// Gets the namespaces in <see cref="Usings"/> whose directive needs <c language="csharp">global::</c>, because an
    /// enclosing namespace could rebind its first segment.
    /// </summary>
    public ImmutableHashSet<string> RootedUsings { get; init; } = [];

    /// <summary>
    /// Gets the directive target for a namespace imported inside the file-scoped namespace.
    /// </summary>
    /// <param name="namespace">The namespace from <see cref="Usings"/>.</param>
    /// <returns>The namespace as the directive spells it.</returns>
    public string Inner(string @namespace) => RootedUsings.Contains(@namespace) ? $"global::{@namespace}" : @namespace;

    /// <summary>
    /// Gets the spelling of a reference.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <returns>The spelling; the fully qualified name for a reference the request did not include.</returns>
    public string Spell(TypeReference reference) => Spellings.GetValueOrDefault(reference) ?? reference.Qualified;
}
