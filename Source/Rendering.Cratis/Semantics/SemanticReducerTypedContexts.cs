// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Shares the selected reducer wrapper declarations between admission, imports and publication.</summary>
internal static class SemanticReducerTypedContexts
{
    internal static IEnumerable<SemanticTypedContextDescriptor> Selected(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var selected = context.SelectedSlices().Select(located => located.Slice.Id).ToHashSet();
        var pairs = slices.Where(located => selected.Contains(located.Slice.Id)).SelectMany(located => located.Slice.Reducers)
            .SelectMany(reducer => reducer.Transitions.Select(transition => (transition.RequirementId, reducer.ReadModel))).ToHashSet();
        return context.Request.TypedContextDescriptors.Where(descriptor => descriptor.Role == SemanticImplementationRole.ReducerTransition &&
                descriptor.OperationId is { } operation && pairs.Contains((descriptor.RequirementId, operation)))
            .DistinctBy(descriptor => (descriptor.RequirementId, descriptor.OperationId));
    }

    internal static IEnumerable<(string Namespace, string Name)> Declarations(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var ns = string.Join('.', context.Domain.Append("TypedContexts"));
        foreach (var descriptor in Selected(context, slices))
        {
            yield return (ns, SemanticTypedContextRenderer.WrapperName(descriptor));
            foreach (var member in descriptor.Members.Where(member => EmitsQueryShape(member.Type, context)))
            {
                yield return (ns, SemanticTypedContextRenderer.LocalShapeName(descriptor, member.Name));
            }
        }
    }

    static bool EmitsQueryShape(SemanticContextType type, SemanticApplicationContext context) =>
        type.Kind == SemanticContextTypeKinds.Shape && type.ModelType is null && type.RuntimeToken is null &&
        type.Shape is { } shape && context.Queries.TryGetValue(shape, out var query) && query.Argument is { } argument &&
        type.Properties.Length == 1 && type.Properties[0] == new SemanticContextProperty(argument.Name, argument.Id, argument.Type);
}
