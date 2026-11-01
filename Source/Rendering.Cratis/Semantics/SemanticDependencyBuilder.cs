// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Walks classified ESM references, retaining owners even for shapes the renderer refuses.</summary>
/// <param name="context">The complete application, not just the selected rendering scope.</param>
/// <param name="graph">The accumulating dependency graph.</param>
internal sealed class SemanticDependencyBuilder(SemanticApplicationContext context, SemanticDependencyGraph graph)
{
    readonly SemanticDependencyProductions _productions = new();
    readonly HashSet<SemanticId> _slices = [.. context.AllSlices.Select(located => located.Slice.Id)];

    internal void Build()
    {
        Walk(context.Application, new(context.Application.Id, SemanticDependencyKind.Declaration, "ExecutableSemanticModel.Application"));
        foreach (var descriptor in context.Request.TypedContextDescriptors.IsDefault ? [] : context.Request.TypedContextDescriptors)
        {
            Walk(descriptor, new(context.Application.Id, SemanticDependencyKind.TypedContext, "ArtifactRenderRequest.TypedContextDescriptors"));
        }
        _productions.ConnectConstraints(context, graph);
    }

    static SemanticDependencyKind EdgeKind(object node, SemanticDependencyKind inherited, SemanticDependencyKind category) => node switch
    {
        SemanticContextSource { EventRevision: not null } => SemanticDependencyKind.HistoricalEvent,
        _ => inherited is SemanticDependencyKind.TypedContext or SemanticDependencyKind.HistoricalEvent ? inherited : category
    };

    static bool IsDefaultArray(object value)
    {
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) && (bool)type.GetProperty("IsDefault")!.GetValue(value)!;
    }

    static bool IsAbsentReference(object node, PropertyInfo property) => node switch
    {
        SemanticTypeReference { Kind: SemanticTypeReferenceKind.Primitive } => property.Name == "Target",
        SemanticReactionTrigger { Kind: SemanticReactionTriggerKind.Startup or SemanticReactionTriggerKind.Shutdown or SemanticReactionTriggerKind.Interval or SemanticReactionTriggerKind.Schedule, Source.IsSet: false } => property.Name == "Source",
        SemanticSpecificationTrigger { Kind: SemanticReactionTriggerKind.Startup or SemanticReactionTriggerKind.Shutdown, Trigger.IsSet: false } => property.Name == "Trigger",
        _ => false
    };

    static bool IsAbsentPrimitive(object node, PropertyInfo property)
    {
        if (property.Name != "Primitive") return false;
        return node switch
        {
            SemanticTypeReference { Primitive: SemanticPrimitiveType.Unknown } reference => reference.Kind is SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType,
            SemanticContextTypeDefinition { Kind: SemanticTypeReferenceKind.CompositeType, Primitive: SemanticPrimitiveType.Unknown } => true,
            _ => false
        };
    }

    void Walk(object? node, SemanticDependencyScope scope)
    {
        if (node is null) return;
        Validate(node, scope);
        switch (node)
        {
            case SemanticId reference:
                graph.Reference(new(scope.Owner, reference, scope.Kind, scope.Member), scope.PropertyTypeOf);
                break;
            case IEnumerable sequence when node is not string:
                WalkSequence(sequence, scope);
                break;
            default:
                if (node.GetType().Namespace == typeof(SemanticApplication).Namespace) WalkDeclaration(node, scope);
                break;
        }
    }

    void Validate(object node, SemanticDependencyScope scope)
    {
        if (!SemanticDependencyVariants.Known(node)) graph.Incomplete(new(SemanticDependencyIncompletenessKind.UnknownVariant, scope.Owner, scope.Member, scope.PropertyTypeOf, $"{scope.Member}: unknown variant '{node.GetType().Name}'."));
    }

    void WalkSequence(IEnumerable sequence, SemanticDependencyScope scope)
    {
        if (IsDefaultArray(sequence)) return;
        foreach (var item in sequence) Walk(item, scope);
    }

    void WalkDeclaration(object node, SemanticDependencyScope scope)
    {
        var owner = SemanticDependencyOwnership.Owner(node, scope.Owner);
        SemanticDependencyOwnership.Register(node, owner, graph);
        _productions.Register(node, owner);
        if (owner != scope.Owner && _slices.Contains(scope.Owner)) graph.Reference(scope.Owner, owner, SemanticDependencyKind.Declaration, "SemanticSlice.declarations");
        if (node is SemanticProjection projection) graph.Reference(projection.ReadModel, projection.Id, SemanticDependencyKind.Projection, "SemanticProjection.ReadModel");
        if (node is SemanticPolicyReference policy) Policy(policy, owner);
        var nested = scope with { Owner = owner, PropertyTypeOf = owner == scope.Owner ? scope.PropertyTypeOf : null };
        foreach (var property in SemanticDependencySurface.Properties(node.GetType())) WalkProperty(node, nested, property);
    }

    void WalkProperty(object node, SemanticDependencyScope scope, PropertyInfo property)
    {
        if (IsAbsentPrimitive(node, property)) return;
        var member = $"{property.DeclaringType!.Name}.{property.Name}";
        var classification = SemanticDependencySurface.Classify(member);
        var value = property.GetValue(node);
        var location = scope with
        {
            Member = member,
            PropertyTypeOf = member == "SemanticProperty.Type" ? scope.Owner : scope.PropertyTypeOf
        };
        if (classification is null) graph.Incomplete(new(SemanticDependencyIncompletenessKind.UnclassifiedMember, scope.Owner, member, location.PropertyTypeOf, $"{member}: unclassified reference-carrying member."));
        else if (classification.Kind is { } category && !IsAbsentReference(node, property)) Walk(value, location with { Kind = EdgeKind(node, scope.Kind, category) });
        else if (!IsAbsentReference(node, property)) Walk(value, location);
    }

    void Policy(SemanticPolicyReference reference, SemanticId owner)
    {
        var matches = context.Application.Policies.Where(policy => policy.Name == reference.Name).ToArray();
        if (matches.Length != 1)
        {
            graph.Incomplete(new(SemanticDependencyIncompletenessKind.UnresolvedPolicy, owner, "SemanticPolicyReference.Name", null, $"SemanticPolicyReference.Name: unresolved or ambiguous policy '{reference.Name}'."));
            return;
        }

        // Policies have no identity: the application owns the declaration, while its condition is examined at each use site.
        graph.Reference(owner, context.Application.Id, SemanticDependencyKind.Policy, "SemanticPolicyReference.Name");
        Walk(matches[0].Condition, new(owner, SemanticDependencyKind.Policy, "SemanticPolicy.Condition"));
    }
}
