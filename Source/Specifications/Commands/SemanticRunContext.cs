// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Commands;

// Chronicle 19.4.7 has a public in-memory event store, but its projection scenarios are bound to
// process-static Defaults.Instance. Until a per-run projection seam ships, retain the exact typed
// facts in a fresh per-spec log instead of pretending Chronicle projected them.

/// <summary>
/// Holds the isolated state for a single semantic command scenario.
/// </summary>
/// <param name="commandType">The runtime command type.</param>
/// <param name="command">The command definition.</param>
/// <param name="specification">The specification fixture.</param>
/// <param name="options">The execution options.</param>
/// <param name="runtimeTypes">The runtime event contracts.</param>
/// <param name="eventStore">The fresh Chronicle in-memory event store.</param>
public sealed class SemanticRunContext(Type commandType, SemanticCommand command, SemanticSpecification specification, SemanticSpecificationRunOptions options, SemanticRuntimeTypes runtimeTypes, EventStoreForTesting eventStore)
{
    readonly List<SemanticSpecificationEvent> _facts = [];

    internal Type CommandType => commandType;
    internal SemanticCommand Command => command;
    internal SemanticSpecification Specification => specification;
    internal IReadOnlyList<SemanticSpecificationEvent> Facts => _facts;
    internal DateTimeOffset Occurred => options.Clock.GetUtcNow();
    internal string Tenant => options.Tenant;

    internal static string Canonical(SemanticValue value) => value switch
    {
        SemanticNullValue => "null",
        SemanticTextValue text => JsonSerializer.Serialize(text.Value),
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        _ => JsonSerializer.Serialize(value)
    };

    internal static bool Empty(SemanticValue value) => value is SemanticNullValue or SemanticTextValue { Value.Length: 0 } or SemanticArrayValue { Values.IsEmpty: true };

    internal static SemanticValue Evaluate(SemanticExpression expression, Dictionary<SemanticId, SemanticValue> inputs) => expression switch
    {
        SemanticValueExpression literal => literal.Value,
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } reference => inputs[reference.Target],
        _ => throw new UnsupportedSemanticMapping()
    };

    internal async Task Append(SemanticSpecificationEvent fact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var instance = runtimeTypes.Materialize(fact);
        var source = fact.EventSource?.Value switch
        {
            SemanticTextValue text => text.Value,
            SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
            SemanticBooleanValue boolean => boolean.Value.ToString(CultureInfo.InvariantCulture),
            _ => throw new UnsupportedSemanticMapping()
        };
        var result = await eventStore.EventLog.Append(new EventSourceId(source), instance, occurred: Occurred);
        if (!result.IsSuccess)
        {
            throw new SemanticAppendFailed($"The in-memory append for '{fact.EventContract}' failed: {string.Join(", ", result.Errors)}");
        }

        _facts.Add(fact);
    }

    internal SemanticSpecificationEvent Produce(SemanticProducedEvent produced)
    {
        var inputs = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        var expression = produced.Destination ?? command.Destination?.Value;
        var destination = expression is null ? specification.When.EventSource!.Value : Evaluate(expression, inputs);
        var identityType = specification.When.EventSource?.Type ?? command.Destination?.Type;
        var identity = identityType is null ? null : new SemanticEventSourceIdentity(identityType, destination);
        var values = produced.Mappings.Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, Evaluate(mapping.Source, inputs))).ToImmutableArray();
        return new(produced.EventContract, values) { EventSource = identity };
    }
}

/// <summary>
/// The exception that is thrown when an in-memory append fails.
/// </summary>
/// <param name="message">The append failure.</param>
public sealed class SemanticAppendFailed(string message) : Exception(message);

/// <summary>
/// The exception that is thrown when an expression escaped admission.
/// </summary>
public sealed class UnsupportedSemanticMapping : Exception
{
    /// <summary>
    /// Creates the failure when a mapping escapes admission.
    /// </summary>
    public UnsupportedSemanticMapping() : base("A mapping escaped semantic admission.")
    {
    }
}
