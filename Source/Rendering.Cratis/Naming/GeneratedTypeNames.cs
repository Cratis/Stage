// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

/// <summary>
/// Inventories top-level C# declarations in each emitted namespace, including validators and constraints.
/// Both render and execution admission use the same inventory. The context inventory the C# import resolver
/// resolves against is these declarations plus the command response types and, once admission has decided which specifications are refused, the classes of
/// the specifications that are rendered; specification classes stay out of the collision admission.
/// </summary>
internal static class GeneratedTypeNames
{
    /// <summary>
    /// Finds generated declarations that collide with each other or with a generated namespace.
    /// </summary>
    /// <param name="application">The application being rendered.</param>
    /// <param name="slices">The selected slices with their module and feature paths.</param>
    /// <param name="constraints">The selected constraints, or every constraint of the selected slices.</param>
    /// <param name="rendersStringsCatalog">Whether the strings catalog is rendered.</param>
    /// <param name="opaquePolicyTypes">Policy runtime types declared outside the modeled namespaces.</param>
    /// <param name="commonNamespace">The namespace, relative to the root, of concepts and composite types.</param>
    /// <param name="eventSources">The event sources to render, or every event source of the application.</param>
    /// <param name="eventSourcesNamespace">The namespace, relative to the root, of event sources.</param>
    /// <param name="policiesNamespace">The namespace, relative to the root, of generated policies.</param>
    /// <returns>Each colliding artifact, the kind of declaration and the colliding name.</returns>
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
        var located = slices.Select(selected => (Path: selected.Path.ToArray(), selected.Slice)).ToArray();
        return Collisions(
            located.Select(selected => (IEnumerable<string>)selected.Path),
            Declarations(
                application,
                located.Select(selected => ((IEnumerable<string>)selected.Path, selected.Slice)),
                constraints,
                rendersStringsCatalog,
                opaquePolicyTypes,
                commonNamespace,
                eventSources,
                eventSourcesNamespace,
                policiesNamespace));
    }

    /// <summary>
    /// Finds declarations that collide with each other or with a namespace a selected slice path declares.
    /// </summary>
    /// <param name="slicePaths">The module, feature and slice paths of the selected slices.</param>
    /// <param name="declarations">The generated declarations, in declaration order.</param>
    /// <returns>Each colliding artifact, the kind of declaration and the colliding name.</returns>
    internal static IEnumerable<(SemanticId Artifact, string Kind, string Name)> Collisions(
        IEnumerable<IEnumerable<string>> slicePaths,
        IEnumerable<GeneratedTypeDeclaration> declarations)
    {
        var namespaces = new HashSet<(string Namespace, string Name)>();
        foreach (var path in slicePaths.Select(path => path.Select(GeneratedPascalCase.From).ToArray()))
        {
            for (var index = 0; index < path.Length; index++)
            {
                namespaces.Add((string.Join('.', path.Take(index)), path[index]));
            }
        }
        var seen = new HashSet<(string Namespace, string Name)>();
        foreach (var declaration in declarations)
        {
            if (!seen.Add((declaration.Namespace, declaration.Name))) yield return (declaration.Artifact, declaration.Kind, declaration.Name);
            if (namespaces.Contains((declaration.Namespace, declaration.Name))) yield return (declaration.Artifact, "Namespace", declaration.Name);
        }
    }

    /// <summary>
    /// Enumerates every top-level type the rendered application declares, in declaration order, with its namespace
    /// relative to the root namespace. Duplicates are kept: a collision is a property of the inventory, not of a name.
    /// </summary>
    /// <param name="application">The application being rendered.</param>
    /// <param name="slices">The selected slices with their module and feature paths.</param>
    /// <param name="constraints">The selected constraints, or every constraint of the selected slices.</param>
    /// <param name="rendersStringsCatalog">Whether the strings catalog is rendered.</param>
    /// <param name="opaquePolicyTypes">Policy runtime types declared outside the modeled namespaces.</param>
    /// <param name="commonNamespace">The namespace, relative to the root, of concepts and composite types.</param>
    /// <param name="eventSources">The event sources to render, or every event source of the application.</param>
    /// <param name="eventSourcesNamespace">The namespace, relative to the root, of event sources.</param>
    /// <param name="policiesNamespace">The namespace, relative to the root, of generated policies.</param>
    /// <param name="selfReduces">Decides, per read model, whether reducer members are emitted on the model rather than as a separate type.</param>
    /// <param name="policyClassName">Gives the policy class of a protected operation; operations that share a policy share the class.</param>
    /// <param name="commandReceiptTime">Whether selected artifacts require the receipt-time helper; null derives it from command mappings.</param>
    /// <returns>The generated declarations.</returns>
    internal static IEnumerable<GeneratedTypeDeclaration> Declarations(
        SemanticApplication application,
        IEnumerable<(IEnumerable<string> Path, SemanticSlice Slice)> slices,
        IEnumerable<(IEnumerable<string> Path, SemanticId Owner, SemanticConstraint Constraint)>? constraints = null,
        bool rendersStringsCatalog = false,
        IEnumerable<(string Namespace, string Name)>? opaquePolicyTypes = null,
        string commonNamespace = "Common",
        IEnumerable<SemanticEventSource>? eventSources = null,
        string eventSourcesNamespace = "EventSources",
        string policiesNamespace = "GeneratedPolicies",
        Func<SemanticId, bool>? selfReduces = null,
        Func<SemanticId, string>? policyClassName = null,
        bool? commandReceiptTime = null)
    {
        var selectedSlices = slices.Select(selected => (Path: selected.Path.Select(GeneratedPascalCase.From).ToArray(), selected.Slice)).ToArray();
        foreach (var source in eventSources ?? application.EventSources)
        {
            foreach (var name in Names(eventSourcesNamespace, EventSourceName(source.Name), source.Id, "EventSource", generated: true)) yield return name;
        }
        if (UsesStreamIds(selectedSlices.Select(slice => slice.Slice)))
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
            foreach (var reaction in slice.Reactions.IsDefault ? [] : slice.Reactions)
            {
                foreach (var name in Names(ns, reaction.Name, reaction.Id, "Reaction")) yield return name;
            }
            foreach (var reducer in slice.Reducers.Where(reducer => selfReduces?.Invoke(reducer.ReadModel) != true))
            {
                foreach (var name in Names(ns, reducer.Name, reducer.ReadModel, "Reducer")) yield return name;
            }
            foreach (var command in slice.Commands)
            {
                foreach (var name in Names(ns, command.Name, command.Id, "Command")) yield return name;
                if (command.Validations.IsEmpty && command.CodeValidations.IsEmpty && command.Requirements.IsEmpty && !HasConstrainedProperty(application, command)) continue;
                foreach (var name in Names(ns, $"{GeneratedPascalCase.From(command.Name)}Validator", command.Id, "Command")) yield return name;
            }
            foreach (var @event in slice.Events)
            {
                foreach (var name in Names(ns, @event.Name, @event.Id, "Event")) yield return name;
            }
            foreach (var readModel in slice.ReadModels)
            {
                foreach (var name in Names(ns, readModel.Name, readModel.Id, "ReadModel")) yield return name;
                if (SemanticVersionFeatures.HasCompositeKey(readModel))
                {
                    foreach (var name in Names(ns, $"{GeneratedPascalCase.From(readModel.Name)}Key", readModel.Id, "ReadModelKey", generated: true)) yield return name;
                }
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
        // adds PolicyValues. Operations that share an effective policy share one `{name}Policy` declaration.
        foreach (var name in Names("GeneratedPolicies", "Registration", application.Id, "Generated")) yield return name;
        if (selectedSlices.Any(located => located.Slice.Commands.Any(command => command.Authorization is not null) ||
            located.Slice.Queries.Any(query => query.Authorization is not null)))
        {
            foreach (var name in Names("GeneratedPolicies", "PolicyValues", application.Id, "Generated")) yield return name;
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var operation in selectedSlices.SelectMany(located => located.Slice.Commands.Where(command => command.Authorization is not null).Select(command => command.Id)
                .Concat(located.Slice.Queries.Where(query => query.Authorization is not null).Select(query => query.Id))))
            {
                if (policyClassName is null || !declared.Add(policyClassName(operation))) continue;
                foreach (var name in Names(policiesNamespace, policyClassName(operation), operation, "Generated", generated: true)) yield return name;
            }
        }
        foreach (var (ns, type) in opaquePolicyTypes ?? [])
        {
            foreach (var name in Names(ns, type, application.Id, "Generated", generated: true)) yield return name;
        }
        if (commandReceiptTime ?? UsesCommandReceiptTime(selectedSlices.SelectMany(located => located.Slice.Commands)))
        {
            foreach (var name in Names("GeneratedCommands", "CommandReceiptTime", application.Id, "Generated")) yield return name;
            foreach (var name in Names("GeneratedCommands", "CommandReceiptTimeUnavailable", application.Id, "Generated")) yield return name;
        }
        if (UsesProtectedReads(selectedSlices.SelectMany(located => located.Slice.Commands)))
        {
            foreach (var name in Names("GeneratedCommands.when_admitting_protected_reads", "when_admitting_protected_reads", application.Id, "Generated", generated: true)) yield return name;
        }
        if (selectedSlices.Any(located => !located.Slice.Reducers.IsEmpty))
        {
            foreach (var name in Names("TypedContexts", "TenantId", application.Id, "Generated")) yield return name;
            foreach (var name in Names("TypedContexts", "ReducerContextValues", application.Id, "Generated")) yield return name;
            foreach (var name in Names("GeneratedTenancy", "PortableTenantValues", application.Id, "Generated")) yield return name;
            foreach (var name in Names("GeneratedTenancy", "AmbiguousTenant", application.Id, "Generated")) yield return name;
        }

        static IEnumerable<GeneratedTypeDeclaration> Names(string ns, string source, SemanticId id, string kind, bool generated = false)
        {
            yield return new(id, kind, ns, generated ? source : GeneratedPascalCase.From(source));
        }
    }

    internal static string EventSourceName(string source)
    {
        var name = GeneratedPascalCase.From(source);

        return source != name && name.EndsWith("EventSource", StringComparison.Ordinal) ? name : $"{name}EventSource";
    }

    internal static bool UsesStreamIds(IEnumerable<SemanticCommand> commands) =>
        commands.Any(command => command.Route is { } route && (route.StreamId is SemanticResolvedExpression || !route.StreamIdParts.IsDefaultOrEmpty));

    internal static bool UsesStreamIds(IEnumerable<SemanticSlice> slices) =>
        slices.Any(slice => UsesStreamIds(slice.Commands) || slice.Specifications.Any(specification =>
            specification.GivenEvents.Concat(specification.ThenEvents).Any(occurrence => occurrence.Route is { } route &&
                (route.StreamId is not null || !route.StreamIdParts.IsDefaultOrEmpty)) ||
            (specification.WhenAppended?.Route is { } appendRoute && (appendRoute.StreamId is not null || !appendRoute.StreamIdParts.IsDefaultOrEmpty))));

    internal static bool UsesProtectedReads(IEnumerable<SemanticCommand> commands) => commands.Any(command => !command.Reads.IsDefaultOrEmpty);

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
