// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed partial class SemanticCommandResultRendering
{
    const string ConditionalEventsType = "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>";

    void RenderConditionalHandle(CSharpCodeBuilder builder, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        var events = ConditionalEvents(wrappedEvent);
        var response = Response();
        var routing = new SemanticCommandRouteRendering(command, context);
        var resultType = response is null ? events.Type : $"({response.Type}, {events.Type})";
        var routeResult = $"global::OneOf.OneOf<{resultType}, global::Cratis.Arc.Validation.ValidationResult>";
        builder.OpenBlock($"public {(routing.HasChecks ? routeResult : resultType)} Handle({Parameters()})");
        if (routing.HasChecks) routing.RenderChecks(builder, routeResult);
        if (HasOccurrence)
        {
            builder.Line($"var occurred = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt(operation);");
        }
        RenderSelectedEvents(builder, wrappedEvent);
        var value = response is null ? events.Value : $"({response.Value}, {events.Value})";
        builder.Line($"return {(routing.HasChecks ? $"{routeResult}.FromT0({value})" : value)};").EndBlock();
    }

    void RenderSelectedEvents(CSharpCodeBuilder builder, Func<SemanticProducedEvent, string> wrappedEvent)
    {
        foreach (var (produced, index) in command.Produces.Select((produced, index) => (produced, index)))
        {
            var condition = produced.When is null ? "true" : SemanticConditionRendering.Render(produced.When, command.Properties, string.Empty, context, PropertyValue);
            builder.Line($"var stageProduces{index} = {condition};");
        }
        builder.Line("var stageEvents = new global::System.Collections.Generic.List<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>();");
        foreach (var (produced, index) in command.Produces.Select((produced, index) => (produced, index)))
        {
            builder.OpenBlock($"if (stageProduces{index})").Line($"stageEvents.Add({wrappedEvent(produced)});").EndBlock();
        }
    }

    ResultExpression ConditionalEvents(Func<SemanticProducedEvent, string> wrappedEvent) =>
        SemanticGeneratedFreshness.Destinations(command, context).Any()
            ? new(SemanticGeneratedFreshness.ResultType, SemanticGeneratedFreshness.Events(command, context, wrappedEvent, "stageEvents", SourceGuard))
            : new(ConditionalEventsType, "stageEvents");

    string SourceGuard(SemanticProperty property)
    {
        var targets = command.Produces.Select((produced, index) => (produced, index))
            .Where(item => SemanticDestinations.Of(command, item.produced) is SemanticResolvedExpression destination && destination.Target == property.Id).ToArray();
        return targets.Any(item => item.produced.When is null) ? "true" : string.Join(" || ", targets.Select(item => $"stageProduces{item.index}"));
    }
}
