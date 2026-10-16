// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Provides strict semantic value-shape admission for specification execution and Host admission.
/// Renderer command properties consume only the top-level check in SemanticCratisAdmission.TypeExists:
/// its own STAGE-ESM-002/003/005/012 type diagnostics cover concept rules, primitives and nested types.
/// Rendering-fidelity limits also belong to the renderer, not to this semantic closure.
/// </summary>
internal static class SemanticValueShapeAdmission
{
    const string UnresolvedType = "The value has an unresolved type.";

    internal static string? Refusal(SemanticApplicationContext context, SemanticTypeReference type) =>
        Closure(context, type).Select(member => TypeRefusal(context, member.Type)).FirstOrDefault(refusal => refusal is not null);

    internal static IEnumerable<(string Path, SemanticTypeReference Type)> Closure(SemanticApplicationContext context, SemanticTypeReference type) =>
        Closure(context, type, string.Empty, []);

    static string? TypeRefusal(SemanticApplicationContext context, SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => PrimitiveRefusal(type.Primitive),
        SemanticTypeReferenceKind.CompositeType => context.Types.ContainsKey(type.Target) ? null : UnresolvedType,
        SemanticTypeReferenceKind.Concept => ConceptRefusal(context, type.Target),
        _ => UnresolvedType
    };

    static string? PrimitiveRefusal(SemanticPrimitiveType primitive) =>
        primitive != SemanticPrimitiveType.Unknown && Enum.IsDefined(primitive) ? null : UnresolvedType;

    static string? ConceptRefusal(SemanticApplicationContext context, SemanticId id)
    {
        if (!context.Concepts.TryGetValue(id, out var concept)) return UnresolvedType;

        return PrimitiveRefusal(concept.Primitive) ?? ConceptRuleRefusal(concept);
    }

    static string? ConceptRuleRefusal(SemanticConcept concept) =>
        concept.Validations.Any(rule => !SemanticRuleValues.CanEvaluate(rule.Kind)) ? "A concept validation rule is not admitted." : null;

    static IEnumerable<(string Path, SemanticTypeReference Type)> Closure(SemanticApplicationContext context, SemanticTypeReference type, string path, HashSet<SemanticId> visited)
    {
        yield return (path, type);
        if (!CanDescend(type, visited)) yield break;
        if (!context.Types.TryGetValue(type.Target, out var composite)) yield break;
        foreach (var property in composite.Properties)
        {
            var memberPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
            foreach (var member in Closure(context, property.Type, memberPath, [.. visited])) yield return member;
        }
    }

    static bool CanDescend(SemanticTypeReference type, HashSet<SemanticId> visited) =>
        type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target);
}
