// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the given events that establish a read-model specification.
/// </summary>
internal static class SemanticReadModelSpecificationGivens
{
    /// <summary>
    /// Renders given facts in their selected order.
    /// </summary>
    /// <param name="scope">The shared source-rendering state.</param>
    /// <param name="givenEvents">The selected given events.</param>
    /// <returns>Whether the rendered values require the Common namespace.</returns>
    internal static bool Render(SemanticReadModelSpecificationScope scope, IReadOnlyList<SemanticSpecificationEvent> givenEvents)
    {
        var (builder, types, context, located) = scope;
        var givenNeedsCommon = false;
        foreach (var given in givenEvents)
        {
            var givenEvent = context.Events[given.EventContract];
            var givenNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(given.EventContract).Path);
            if (!string.Equals(givenNamespace, SliceNaming.Namespace(context.RootNamespace, located.Path), StringComparison.Ordinal))
            {
                builder.Using(givenNamespace);
            }

            var givenArguments = givenEvent.Properties.Select(property =>
                types.Value(given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            var givenSource = given.EventSource;
            givenNeedsCommon |= (givenSource is not null && SemanticTypeSystem.ValueNeedsCommon(givenSource.Value, givenSource.Type)) ||
                givenEvent.Properties.Any(property => SemanticTypeSystem.ValueNeedsCommon(
                    given.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
            builder.Line($"await _scenario.Given.ForEventSource({SemanticSpecificationEventSources.Expression(context, types, given)}){SemanticSpecificationRouteRendering.Seed(given.Route, context, types)}.Events(new {types.EventType(givenEvent)}({string.Join(", ", givenArguments)}));");
        }

        return givenNeedsCommon;
    }
}
