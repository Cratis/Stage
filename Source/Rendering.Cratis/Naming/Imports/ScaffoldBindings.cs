// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Preserves proved scaffold-scope spellings while allowing aliases for other collisions.
/// Namespace membership never proves a bare name: FileImports supplies the final binding proof.
/// </summary>
/// <param name="request">The file whose bindings are being resolved.</param>
/// <param name="catalog">The framework names available to the project.</param>
internal sealed class ScaffoldBindings(ImportRequest request, FrameworkNameCatalog catalog)
{
    readonly HashSet<string> _globals = request.HasScaffoldGlobalUsings
        ? catalog.ScaffoldGlobalUsings(request.Project).ToHashSet(StringComparer.Ordinal)
        : [];

    public bool KeepsBindings(IReadOnlyDictionary<TypeReference, string> before, IReadOnlyDictionary<TypeReference, string> after) =>
        before.Where(pair => pair.Value != pair.Key.Qualified && HasCollision(pair.Key))
            .All(pair => after[pair.Key] == pair.Value);

    public bool NeedsAlias(TypeReference reference) => !HasCollision(reference);

    bool HasCollision(TypeReference reference) => _globals.Any(@namespace =>
        catalog.TypesNamed(@namespace, reference.Name, reference.Arity, request.Project).Any() ||
        request.GeneratedTypes.Any(type => type.Namespace == @namespace && type.Name == reference.Name && type.Arity == reference.Arity));
}
