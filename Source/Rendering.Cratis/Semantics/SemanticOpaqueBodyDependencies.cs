// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.Validation;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal enum SemanticOpaqueBodyKind
{
    RulePredicate,
    CodeValidation,
    Reducer,
    Policy,
    Handler,
    Reaction,
    Performer
}

internal sealed record SemanticOpaqueBodyDependency(SemanticOpaqueBodyKind Kind, string Name, SemanticTypedContextDescriptor Descriptor);

/// <summary>
/// Admits rule-dependent generated specifications using their then-blocks as the oracle.
/// Other opaque-body roles retain their existing admission gates.
/// </summary>
internal static class SemanticOpaqueBodyDependencies
{
    internal static IEnumerable<SemanticOpaqueBodyDependency> Reached(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.When is { } action && context.Commands.TryGetValue(action.Command, out var command) ? ForCommand(context, command) : [];

    internal static IEnumerable<SemanticOpaqueBodyDependency> ForCommand(SemanticApplicationContext context, SemanticCommand command)
    {
        var owners = new HashSet<SemanticId> { command.Id };
        foreach (var property in command.Properties) AddOwner(property.Type, context, owners);
        return context.Request.TypedContextDescriptors.Where(descriptor => descriptor.OperationId is { } owner && owners.Contains(owner) &&
                descriptor.Role is SemanticImplementationRole.RulePredicate or SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation)
            .Select(descriptor => new SemanticOpaqueBodyDependency(
                descriptor.Role == SemanticImplementationRole.RulePredicate ? SemanticOpaqueBodyKind.RulePredicate : SemanticOpaqueBodyKind.CodeValidation,
                RuleName(context, descriptor),
                descriptor));
    }

    internal static string? Rejection(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (SemanticSpecificationClock.TryGet(specification, out _)) return null;
        var dependency = Reached(context, specification).FirstOrDefault(dependency => SemanticValidationContextRuntime.ReadsOccurred(context, dependency.Descriptor));
        return dependency is null ? null : $"rule '{dependency.Name}' reads context.Occurred; add `given clock` so the rendered rule decides deterministically";
    }

    internal static IEnumerable<SemanticSpecification> WithoutUncontrolledClockSpecifications(
        IEnumerable<SemanticSpecification> specifications,
        SemanticApplicationContext context,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in specifications)
        {
            if (Rejection(context, specification) is { } rejection)
            {
                diagnostics.Add(new(
                    "STAGE-ESM-011",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' cannot render: {rejection}.",
                    specification.Id));
                continue;
            }
            yield return specification;
        }
    }

    static string RuleName(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var owner = descriptor.OperationId!.Value;
        if (context.Commands.TryGetValue(owner, out var command))
            return command.Validations.FirstOrDefault(rule => rule.RequirementId == descriptor.RequirementId)?.Name ?? command.Name;
        var concept = context.Concepts[owner];
        return concept.Validations.FirstOrDefault(rule => rule.RequirementId == descriptor.RequirementId)?.Name ?? concept.Name;
    }

    static void AddOwner(SemanticTypeReference type, SemanticApplicationContext context, HashSet<SemanticId> owners)
    {
        if (type.Kind == SemanticTypeReferenceKind.Primitive || !owners.Add(type.Target)) return;
        if (type.Kind == SemanticTypeReferenceKind.CompositeType && context.Types.TryGetValue(type.Target, out var composite))
        {
            foreach (var property in composite.Properties) AddOwner(property.Type, context, owners);
        }
    }
}
