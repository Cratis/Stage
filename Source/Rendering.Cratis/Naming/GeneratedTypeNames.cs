// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Inventories top-level C# declarations in each emitted namespace, including validators and constraints.
/// Both render and execution admission use the same inventory.
/// </summary>
internal static class GeneratedTypeNames
{
    internal static IEnumerable<(SemanticId Artifact, string Kind, string Name)> Collisions(
        SemanticApplication application,
        IEnumerable<(IEnumerable<string> Path, SemanticSlice Slice)> slices,
        IEnumerable<(IEnumerable<string> Path, SemanticId Owner, SemanticConstraint Constraint)>? constraints = null,
        bool rendersStringsCatalog = false,
        IEnumerable<(string Namespace, string Name)>? opaquePolicyTypes = null,
        string commonNamespace = "Common",
        IEnumerable<SemanticEventSource>? eventSources = null,
        string eventSourcesNamespace = "EventSources",
        string policiesNamespace = "GeneratedPolicies")
    {
        var selectedSlices = slices.Select(selected => (Path: selected.Path.Select(GeneratedPascalCase.From).ToArray(), selected.Slice)).ToArray();
        var namespaces = new HashSet<(string Namespace, string Name)>();
        foreach (var (path, _) in selectedSlices)
        {
            for (var index = 0; index < path.Length; index++)
            {
                namespaces.Add((string.Join('.', path.Take(index)), path[index]));
            }
        }
        var seen = new HashSet<(string Namespace, string Name)>();
        foreach (var source in eventSources ?? application.EventSources)
        {
            foreach (var name in Names(eventSourcesNamespace, EventSourceName(source.Name), source.Id, "EventSource", generated: true)) yield return name;
        }
        if (UsesStreamIds(selectedSlices.SelectMany(slice => slice.Slice.Commands)))
        {
            foreach (var name in Names("GeneratedEventSources", "StreamIds", application.Id, "Generated")) yield return name;
        }
        foreach (var concept in application.Concepts)
        {
            foreach (var name in Names(commonNamespace, concept.Name, concept.Id, "Concept")) yield return name;
            if (!concept.Values.IsEmpty || concept.Validations.IsEmpty) continue;
            foreach (var name in Names(commonNamespace, $"{GeneratedPascalCase.From(concept.Name)}Validator", concept.Id, "Concept")) yield return name;
        }
        foreach (var type in application.Types)
        {
            foreach (var name in Names(commonNamespace, type.Name, type.Id, "Type")) yield return name;
        }
        foreach (var (path, slice) in selectedSlices)
        {
            var ns = string.Join('.', path);
            foreach (var reducer in slice.Reducers)
            {
                foreach (var name in Names(ns, reducer.Name, reducer.ReadModel, "Reducer")) yield return name;
            }
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
                var projectionName = GeneratedPascalCase.From(projection.Name);
                var implementationName = slice.ReadModels.Any(readModel => GeneratedPascalCase.From(readModel.Name) == projectionName)
                    ? $"{projectionName}Projection" : projectionName;
                foreach (var name in Names(ns, implementationName, projection.Id, "Projection")) yield return name;
            }
        }
        foreach (var (path, owner, constraint) in constraints ?? selectedSlices.SelectMany(located => located.Slice.Constraints.Select(constraint => ((IEnumerable<string>)located.Path, located.Slice.Id, constraint))))
        {
            var ns = string.Join('.', path.Select(GeneratedPascalCase.From));
            foreach (var name in Names(ns, constraint.Name, owner, "Constraint")) yield return name;
        }

        // Declarations the renderer adds outside the modeled namespaces. A module, feature or slice
        // with the same generated name would declare a namespace next to one of these types.
        if (rendersStringsCatalog)
        {
            foreach (var name in Names(string.Empty, "GeneratedStrings", application.Id, "Generated")) yield return name;
        }

        // The scaffold declares Registration; the selection-independent Policies.cs implements it and
        // adds PolicyValues. Each protected operation has a separate policy declaration.
        foreach (var name in Names("GeneratedPolicies", "Registration", application.Id, "Generated")) yield return name;
        if (selectedSlices.Any(located => located.Slice.Commands.Any(command => command.Authorization is not null) ||
            located.Slice.Queries.Any(query => query.Authorization is not null)))
        {
            foreach (var name in Names("GeneratedPolicies", "PolicyValues", application.Id, "Generated")) yield return name;
            foreach (var operation in selectedSlices.SelectMany(located => located.Slice.Commands.Where(command => command.Authorization is not null).Select(command => command.Id)
                .Concat(located.Slice.Queries.Where(query => query.Authorization is not null).Select(query => query.Id))))
            {
                var policyName = $"StagePolicy_{operation.ToString().Replace('-', '_').Replace(':', '_')}";
                foreach (var name in Names(policiesNamespace, policyName, operation, "Generated", generated: true)) yield return name;
            }
        }
        foreach (var (ns, type) in opaquePolicyTypes ?? [])
        {
            foreach (var name in Names(ns, type, application.Id, "Generated", generated: true)) yield return name;
        }
        if (UsesCommandReceiptTime(selectedSlices.SelectMany(located => located.Slice.Commands)))
        {
            foreach (var name in Names("GeneratedCommands", "CommandReceiptTime", application.Id, "Generated")) yield return name;
            foreach (var name in Names("GeneratedCommands", "CommandReceiptTimeUnavailable", application.Id, "Generated")) yield return name;
        }
        if (selectedSlices.Any(located => !located.Slice.Reducers.IsEmpty))
        {
            foreach (var name in Names("TypedContexts", "TenantId", application.Id, "Generated")) yield return name;
            foreach (var name in Names("TypedContexts", "ReducerContextValues", application.Id, "Generated")) yield return name;
            foreach (var name in Names("GeneratedTenancy", "PortableTenantValues", application.Id, "Generated")) yield return name;
            foreach (var name in Names("GeneratedTenancy", "AmbiguousTenant", application.Id, "Generated")) yield return name;
        }

        IEnumerable<(SemanticId Artifact, string Kind, string Name)> Names(string ns, string source, SemanticId id, string kind, bool generated = false)
        {
            var name = generated ? source : GeneratedPascalCase.From(source);
            if (!seen.Add((ns, name))) yield return (id, kind, name);
            if (namespaces.Contains((ns, name))) yield return (id, "Namespace", name);
        }
    }

    internal static string EventSourceName(string source)
    {
        var name = GeneratedPascalCase.From(source);
        return name.EndsWith("EventSource", StringComparison.Ordinal) ? name : $"{name}EventSource";
    }

    internal static bool UsesStreamIds(IEnumerable<SemanticCommand> commands) =>
        commands.Any(command => command.Route is { } route && (route.StreamId is SemanticResolvedExpression || !route.StreamIdParts.IsDefaultOrEmpty));

    internal static bool UsesCommandReceiptTime(IEnumerable<SemanticCommand> commands) =>
        commands.Any(command => command.Produces.Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred })));

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
