// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications;

// Foreign fixture sources are text even when the capture key is numeric.
internal static class SemanticCaptureSources
{
    internal static SemanticSpecification Normalize(SemanticExecutionPlan plan, SemanticSpecification specification) => specification with
    {
        GivenEvents = [.. specification.GivenEvents.Select(fact => Normalize(plan, fact))],
        WhenAppended = specification.WhenAppended is { } appended ? Normalize(plan, appended) : null
    };

    internal static SemanticSpecificationEvent Normalize(SemanticExecutionPlan plan, SemanticSpecificationEvent fact)
    {
        if (fact.Route is not null) return fact;
        var contract = plan.Events[fact.EventContract];
        if (contract.Origin is null) return fact;
        foreach (var capture in plan.RunningCaptures.Where(capture => UsesSource(capture, fact.EventContract)).OrderBy(capture => capture.Id.ToString(), StringComparer.Ordinal))
        {
            fact = Normalize(capture, contract, fact);
        }
        return fact;
    }

    internal static string? Mismatch(SemanticCapture capture, string eventName, SemanticValue source, SemanticValue key) =>
        capture.EventsSource is { KeyIsEventSource: true } && !string.Equals(KeyText(source), KeyText(key), StringComparison.Ordinal)
            ? $"Capture '{capture.Name}' keys by event source, but event '{eventName}' from source '{KeyText(source)}' carries key '{KeyText(key)}'."
            : null;

    static bool UsesSource(SemanticCapture capture, SemanticId contract) => capture.EventsSource is { KeyIsEventSource: true } source && source.Events.Contains(contract);

    static SemanticSpecificationEvent Normalize(SemanticCapture capture, SemanticEventContract contract, SemanticSpecificationEvent fact)
    {
        var property = contract.Properties.Single(property => property.Name == capture.Key);
        var key = fact.Values.SingleOrDefault(value => value.TargetProperty == property.Id)?.Value;
        if (key is null or SemanticNullValue) throw new InvalidSemanticContract($"Capture '{capture.Name}' received event '{contract.Name}' with an empty key '{capture.Key}'.");
        if (!UsableKey(key)) return fact;
        if (fact.EventSource?.Value is null or SemanticNullValue)
        {
            return fact with { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text(KeyText(key))) };
        }
        if (Mismatch(capture, contract.Name, fact.EventSource.Value, key) is { } mismatch) throw new InvalidSemanticContract(mismatch);
        return fact;
    }

    static bool UsableKey(SemanticValue value) => value switch
    {
        SemanticTextValue text => !string.IsNullOrWhiteSpace(text.Value),
        SemanticNumberValue => true,
        _ => false
    };

    static SemanticSpecificationAppend Normalize(SemanticExecutionPlan plan, SemanticSpecificationAppend appended)
    {
        var fact = Normalize(plan, new SemanticSpecificationEvent(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route });
        return appended with { EventSource = fact.EventSource };
    }

    static string KeyText(SemanticValue value) => value switch
    {
        SemanticTextValue text => text.Value,
        SemanticNumberValue number => number.Value.ToString("0.############################", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
