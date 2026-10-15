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
    internal static string? Refusal(SemanticApplicationContext context, SemanticTypeReference type)
    {
        foreach (var (_, member) in Closure(context, type))
        {
            switch (member.Kind)
            {
                case SemanticTypeReferenceKind.Primitive when member.Primitive != SemanticPrimitiveType.Unknown && Enum.IsDefined(member.Primitive):
                case SemanticTypeReferenceKind.CompositeType when context.Types.ContainsKey(member.Target):
                    break;
                case SemanticTypeReferenceKind.Concept when context.Concepts.TryGetValue(member.Target, out var concept):
                    if (concept.Primitive == SemanticPrimitiveType.Unknown || !Enum.IsDefined(concept.Primitive)) return "The value has an unresolved type.";
                    if (concept.Validations.Any(rule => !SemanticRuleValues.CanEvaluate(rule.Kind))) return "A concept validation rule is not admitted.";
                    break;
                default:
                    return "The value has an unresolved type.";
            }
        }

        return null;
    }

    internal static IEnumerable<(string Path, SemanticTypeReference Type)> Closure(SemanticApplicationContext context, SemanticTypeReference type) =>
        Closure(context, type, string.Empty, []);

    static IEnumerable<(string Path, SemanticTypeReference Type)> Closure(SemanticApplicationContext context, SemanticTypeReference type, string path, HashSet<SemanticId> visited)
    {
        yield return (path, type);
        if (type.Kind != SemanticTypeReferenceKind.CompositeType || !visited.Add(type.Target) || !context.Types.TryGetValue(type.Target, out var composite)) yield break;
        foreach (var property in composite.Properties)
        {
            var memberPath = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
            foreach (var member in Closure(context, property.Type, memberPath, [.. visited])) yield return member;
        }
    }
}
