// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Provides the single source of semantic value-shape admission for the executor and Host.
/// The renderer keeps its own top-level check and STAGE-ESM-002/003/005/012 type diagnostics.
/// The conformance divergence guard proves renderer-admits implies run-admits for well-formed
/// composite command shapes without opaque rules. Rendering-fidelity limits stay renderer-only.
/// </summary>
internal static class SemanticValueShapeAdmission
{
    const string UnresolvedType = "The value has an unresolved type.";

    internal static string? Refusal(SemanticApplicationContext context, SemanticTypeReference type) =>
        Refusal(context, type, validateConceptRules: true);

    // Events are already facts: their payload shape must resolve, but command-time concept rules do not run.
    internal static string? PayloadRefusal(SemanticApplicationContext context, SemanticTypeReference type) =>
        Refusal(context, type, validateConceptRules: false);

    internal static IEnumerable<(string Path, SemanticTypeReference Type)> Closure(SemanticApplicationContext context, SemanticTypeReference type) =>
        Closure(context, type, string.Empty, []);

    static string? Refusal(SemanticApplicationContext context, SemanticTypeReference type, bool validateConceptRules) =>
        Closure(context, type).Select(member => TypeRefusal(context, member.Type, validateConceptRules)).FirstOrDefault(refusal => refusal is not null);

    static string? TypeRefusal(SemanticApplicationContext context, SemanticTypeReference type, bool validateConceptRules) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => PrimitiveRefusal(type.Primitive),
        SemanticTypeReferenceKind.CompositeType => context.Types.ContainsKey(type.Target) ? null : UnresolvedType,
        SemanticTypeReferenceKind.Concept => ConceptRefusal(context, type.Target, validateConceptRules),
        _ => UnresolvedType
    };

    static string? PrimitiveRefusal(SemanticPrimitiveType primitive) =>
        primitive != SemanticPrimitiveType.Unknown && Enum.IsDefined(primitive) ? null : UnresolvedType;

    static string? ConceptRefusal(SemanticApplicationContext context, SemanticId id, bool validateConceptRules)
    {
        if (!context.Concepts.TryGetValue(id, out var concept)) return UnresolvedType;

        return PrimitiveRefusal(concept.Primitive) ?? (validateConceptRules ? ConceptRuleRefusal(concept) : null);
    }

    static string? ConceptRuleRefusal(SemanticConcept concept) =>
        concept.Validations.Any(rule => SemanticValidationRules.Check(rule, SemanticValue.Null).Status == SemanticRuleStatus.NotEvaluable) ? "A concept validation rule is not admitted." : null;

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
