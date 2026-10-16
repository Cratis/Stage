// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Shares semantic value-shape admission between rendering, specification execution and Host admission.
/// Rendering-fidelity limits belong to the renderer, not to this semantic closure.
/// </summary>
internal static class SemanticValueShapeAdmission
{
    const string UnresolvedType = "The value has an unresolved type.";

    // Rendering defers future primitive enum values to its existing STAGE-ESM-012 emission diagnostic.
    // Execution and Host admission keep the default strict check before materializing values.
    internal static string? Refusal(SemanticApplicationContext context, SemanticTypeReference type, bool deferUnhandledPrimitives = false) =>
        Closure(context, type).Select(member => TypeRefusal(context, member.Type, deferUnhandledPrimitives)).FirstOrDefault(refusal => refusal is not null);

    internal static IEnumerable<(string Path, SemanticTypeReference Type)> Closure(SemanticApplicationContext context, SemanticTypeReference type) =>
        Closure(context, type, string.Empty, []);

    static string? TypeRefusal(SemanticApplicationContext context, SemanticTypeReference type, bool deferUnhandledPrimitives) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => PrimitiveRefusal(type.Primitive, deferUnhandledPrimitives),
        SemanticTypeReferenceKind.CompositeType => context.Types.ContainsKey(type.Target) ? null : UnresolvedType,
        SemanticTypeReferenceKind.Concept => ConceptRefusal(context, type.Target, deferUnhandledPrimitives),
        _ => UnresolvedType
    };

    static string? PrimitiveRefusal(SemanticPrimitiveType primitive, bool deferUnhandledPrimitives) =>
        primitive != SemanticPrimitiveType.Unknown && (deferUnhandledPrimitives || Enum.IsDefined(primitive)) ? null : UnresolvedType;

    static string? ConceptRefusal(SemanticApplicationContext context, SemanticId id, bool deferUnhandledPrimitives)
    {
        if (!context.Concepts.TryGetValue(id, out var concept)) return UnresolvedType;

        return PrimitiveRefusal(concept.Primitive, deferUnhandledPrimitives) ?? ConceptRuleRefusal(concept);
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
