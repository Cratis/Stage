// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Inventories top-level C# declarations in each emitted namespace, including generated validators.
/// Both render and execution admission use the same inventory.
/// </summary>
internal static class GeneratedTypeNames
{
    internal static IEnumerable<(SemanticId Artifact, string Kind, string Name)> Collisions(
        SemanticApplication application,
        IEnumerable<(IEnumerable<string> Path, SemanticSlice Slice)> slices)
    {
        var seen = new HashSet<(string Namespace, string Name)>();
        foreach (var concept in application.Concepts)
        {
            foreach (var name in Names("Common", concept.Name, concept.Id, "Concept")) yield return name;
            if (!concept.Values.IsEmpty || concept.Validations.IsEmpty) continue;
            foreach (var name in Names("Common", $"{GeneratedPascalCase.From(concept.Name)}Validator", concept.Id, "Concept")) yield return name;
        }
        foreach (var type in application.Types)
        {
            foreach (var name in Names("Common", type.Name, type.Id, "Type")) yield return name;
        }
        foreach (var (path, slice) in slices)
        {
            var ns = string.Join('.', path.Select(GeneratedPascalCase.From));
            foreach (var command in slice.Commands)
            {
                foreach (var name in Names(ns, command.Name, command.Id, "Command")) yield return name;
                if (command.Validations.IsEmpty && command.Requirements.IsEmpty && !HasConstrainedProperty(application, command)) continue;
                foreach (var name in Names(ns, $"{GeneratedPascalCase.From(command.Name)}Validator", command.Id, "Command")) yield return name;
            }
            foreach (var @event in slice.Events)
            {
                foreach (var name in Names(ns, @event.Name, @event.Id, "Event")) yield return name;
            }
            foreach (var readModel in slice.ReadModels)
            {
                foreach (var name in Names(ns, readModel.Name, readModel.Id, "ReadModel")) yield return name;
            }
            foreach (var projection in slice.Projections.Where(projection => projection.Scope is not null))
            {
                foreach (var name in Names(ns, projection.Name, projection.Id, "Projection")) yield return name;
            }
        }

        IEnumerable<(SemanticId Artifact, string Kind, string Name)> Names(string ns, string source, SemanticId id, string kind)
        {
            var name = GeneratedPascalCase.From(source);
            if (!seen.Add((ns, name))) yield return (id, kind, name);
        }
    }

    internal static IEnumerable<(IEnumerable<string> Path, SemanticSlice Slice)> AllSlices(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => Feature(feature, [module.Name, feature.Name])));

    static IEnumerable<(IEnumerable<string> Path, SemanticSlice Slice)> Feature(SemanticFeature feature, string[] path) =>
        feature.Slices.Select(slice => ((IEnumerable<string>)[.. path, slice.Name], slice))
            .Concat(feature.Features.SelectMany(child => Feature(child, [.. path, child.Name])));

    static bool HasConstrainedProperty(SemanticApplication application, SemanticCommand command) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => Constraints(feature)))
            .Where(constraint => constraint.Kind == SemanticConstraintKind.UniquePropertyValue)
            .SelectMany(constraint => constraint.Targets)
            .Any(target => command.Produces.Any(produced => produced.EventContract == target.EventContract &&
                produced.Mappings.Any(mapping => target.Properties.Contains(mapping.TargetProperty) &&
                    command.Properties.Any(property => mapping.Source is SemanticResolvedExpression source && property.Id == source.Target &&
                        !command.Validations.Any(rule => rule.Property == property.Id && rule.Kind == SemanticValidationRuleKind.NotEmpty)))));

    static IEnumerable<SemanticConstraint> Constraints(SemanticFeature feature) =>
        feature.Slices.SelectMany(slice => slice.Constraints).Concat(feature.Features.SelectMany(Constraints));
}
