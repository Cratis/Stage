// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Assigns id-less members to their enclosing owner; reducers belong to their target read model.</summary>
internal static class SemanticDependencyOwnership
{
    static readonly HashSet<string> _declarations = new(StringComparer.Ordinal)
    {
        "SemanticApplication", "SemanticModule", "SemanticFeature", "SemanticSlice", "SemanticConcept", "SemanticCompositeType",
        "SemanticEventContract", "SemanticCommand", "SemanticReadModel", "SemanticProjection", "SemanticKeyedQuery",
        "SemanticSpecification", "SemanticReaction", "SemanticCapture", "SemanticApplicationTrigger", "SemanticEventSource", "SemanticEventStream"
    };

    static readonly HashSet<string> _members = new(StringComparer.Ordinal)
    {
        "SemanticProperty", "SemanticReadModelQueryArgument", "SemanticQueryFilter", "SemanticQueryKeyPart"
    };

    internal static bool DeclaresIdentity(Type? type) => type is not null && (_declarations.Contains(type.Name) || _members.Contains(type.Name));

    internal static SemanticId Owner(object node, SemanticId enclosing) => node switch
    {
        SemanticReducer reducer => reducer.ReadModel,
        SemanticTypedContextDescriptor descriptor => descriptor.OperationId ?? enclosing,
        _ => _declarations.Contains(node.GetType().Name) ? Identity(node) : enclosing
    };

    internal static void Register(object node, SemanticId owner, SemanticDependencyGraph graph)
    {
        if (DeclaresIdentity(node.GetType())) graph.Declare(Identity(node), owner);
    }

    static SemanticId Identity(object node) => (SemanticId)node.GetType().GetProperty("Id")!.GetValue(node)!;
}
