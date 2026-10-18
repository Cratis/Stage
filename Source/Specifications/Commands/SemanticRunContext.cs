// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Holds the isolated state for a single semantic command scenario.
/// </summary>
/// <param name="commandType">The runtime command type.</param>
/// <param name="command">The command definition.</param>
/// <param name="specification">The specification fixture.</param>
/// <param name="options">The execution options.</param>
/// <param name="runtimeTypes">The runtime event contracts.</param>
/// <param name="eventLog">The fresh Chronicle in-memory event log.</param>
/// <param name="plan">The model's execution plan.</param>
/// <param name="parent">The scenario state shared by an invoked command.</param>
internal sealed class SemanticRunContext(Type commandType, SemanticCommand command, SemanticSpecification specification, SemanticSpecificationRunOptions options, SemanticRuntimeTypes runtimeTypes, IEventLog eventLog, SemanticExecutionPlan plan, SemanticRunContext? parent = null)
{
    readonly List<SemanticSpecificationEvent> _facts = parent?._facts ?? [];
    readonly List<SemanticValue> _destinations = parent?._destinations ?? [];
    readonly List<DateTimeOffset?> _occurrences = parent?._occurrences ?? [];
    SemanticFixtureRoute? _commandRoute;

    internal SemanticRunReads Reads { get; } = new(runtimeTypes);
    internal Type CommandType => commandType;
    internal SemanticCommand Command => command;
    internal SemanticSpecification Specification => specification;
    internal SemanticSpecification WorldFixture { get; } = parent?.WorldFixture ?? specification;
    internal SemanticExecutionPlan Plan => plan;
    internal IReadOnlyList<SemanticSpecificationEvent> Facts => _facts;
    internal IReadOnlyList<SemanticValue> Destinations => _destinations;
    internal IReadOnlyList<DateTimeOffset?> Occurrences => _occurrences;
    internal DateTimeOffset Occurred => AuthoredOccurrence ?? options.Clock.GetUtcNow();
    internal DateTimeOffset? AuthoredOccurrence => OccurrenceOf(specification);
    internal string Tenant => options.Tenant;
    internal SemanticExecutionResponse? Response { get; private set; }
    internal bool HandlerBodyReached { get; set; }

    // An absent authored fixture must stay absent in semantic reactions, regardless of the injectable storage clock.
    internal static DateTimeOffset? OccurrenceOf(SemanticSpecification fixture) =>
        SemanticSpecificationClock.TryGetOccurrence(fixture, out var instant) ? instant : null;

    internal static string Canonical(SemanticValue value) => value switch
    {
        SemanticNullValue => "null",
        SemanticTextValue text => JsonSerializer.Serialize(text.Value),
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        _ => JsonSerializer.Serialize(value)
    };

    internal static string Canonical(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext context)
    {
        if (SemanticValueShapeAdmission.Refusal(context, type) is not null) throw new UnsupportedSemanticMapping();
        if (value is SemanticNullValue) return "null";
        if (type.IsCollection && value is SemanticArrayValue array)
        {
            return CanonicalArray(array, type, context);
        }
        if (type.Kind == SemanticTypeReferenceKind.CompositeType && value is SemanticCompositeValue composite)
        {
            return CanonicalComposite(composite, type, context);
        }

        return Canonical(value);
    }

    internal static bool Empty(SemanticValue value) =>
        SemanticValidationRules.Check(new SemanticValidationRule(default, SemanticValidationRuleKind.NotEmpty, null, null), value).Status == SemanticRuleStatus.Failed;

    internal static SemanticValue Evaluate(SemanticExpression expression, Dictionary<SemanticId, SemanticValue> inputs, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null) => expression switch
    {
        SemanticValueExpression literal => literal.Value,
        SemanticReadPathExpression read => reads![read.Read].GetValueOrDefault(read.PropertyId, SemanticValue.Null),
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } reference => inputs[reference.Target],
        _ => throw new UnsupportedSemanticMapping()
    };

    internal void CompleteResponse()
    {
        var inputs = Inputs();
        Response = command.Response switch
        {
            SemanticScalarCommandResponse scalar => new SemanticScalarExecutionResponse(inputs[scalar.Source]),
            SemanticRecordCommandResponse record => new SemanticRecordExecutionResponse([.. record.Fields.Select(field => new SemanticExecutionResponseField(field.Name, inputs[field.Source]))]),
            _ => null
        };
    }

    internal Task Append(SemanticSpecificationEvent fact, CancellationToken cancellationToken) => Append(fact, fact.EventSource?.Value ?? SemanticValue.Null, cancellationToken);

    internal Task Append(SemanticSpecificationEvent fact, SemanticValue destination, CancellationToken cancellationToken) =>
        Append(fact, destination, AuthoredOccurrence, cancellationToken);

    internal Task Append(SemanticSpecificationEvent fact, SemanticValue destination, DateTimeOffset? occurred, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Append(Prepare(fact, destination, occurred), cancellationToken);
    }

    internal SemanticPreparedFact Prepare(SemanticSpecificationEvent fact, SemanticValue destination, DateTimeOffset? occurred)
    {
        var instance = runtimeTypes.Materialize(fact);
        var source = StorageKey(destination);
        var route = SemanticRunRoutes.Format(plan, fact.Route);

        return new(
            fact,
            destination,
            instance,
            new EventSourceId(source),
            route is null ? EventStreamType.All : new EventStreamType(route.StreamKind),
            route?.StreamId is null ? EventStreamId.Default : new EventStreamId(route.StreamId),
            route is null ? EventSourceType.Default : new EventSourceType(route.SourceKind),
            occurred);
    }

    internal async Task Append(SemanticPreparedFact prepared, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fact = prepared.Fact;
        var result = await eventLog.Append(
            prepared.Source,
            prepared.Instance,
            eventStreamType: prepared.StreamType,
            eventStreamId: prepared.StreamId,
            eventSourceType: prepared.SourceType,
            occurred: prepared.Occurred ?? Occurred);
        if (!result.IsSuccess)
        {
            throw new SemanticAppendFailed($"The in-memory append for '{fact.EventContract}' failed: {string.Join(", ", result.Errors)}");
        }

        _facts.Add(fact);
        _destinations.Add(prepared.Destination);
        _occurrences.Add(prepared.Occurred);
    }

    internal bool TryResolveRoute(out StreamIdFormatFailure failure)
    {
        var inputs = specification.When!.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        _commandRoute = command.Route is not { } route ? null : new(route.Source, route.Stream)
        {
            StreamId = route.StreamId is null ? null : Evaluate(route.StreamId, inputs),
            StreamIdParts = [.. route.StreamIdParts.Select(part => new SemanticFixtureRoutePart(part.Part, Evaluate(part.Value, inputs)))]
        };

        return SemanticRunRoutes.TryFormat(plan.Model.Application, _commandRoute, out _, out failure);
    }

    internal bool Holds(SemanticProducedEvent produced) => produced.When is null || SemanticRuleEvaluation.Condition(produced.When, Inputs(), Reads.Values, SemanticDateComparisons.Participants(Plan.Model.Application, Command));

    internal (SemanticSpecificationEvent Fact, SemanticValue Destination) Produce(SemanticProducedEvent produced)
    {
        var inputs = Inputs();
        var expression = produced.Destination ?? command.Destination?.Value;
        var destination = expression is null ? specification.When!.EventSource!.Value : Evaluate(expression, inputs, Reads.Values);
        var identityType = expression is SemanticResolvedExpression resolved
            ? command.Properties.Single(property => property.Id == resolved.Target).Type
            : command.Destination?.Type ?? specification.When!.EventSource?.Type;
        var identity = plan.Model.SemanticVersion != SemanticVersion.V1 && identityType is not null ? new SemanticEventSourceIdentity(identityType, destination) : null;
        var values = produced.Mappings.Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }
            ? SemanticValue.Text(Occurred.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))
            : Evaluate(mapping.Source, inputs, Reads.Values))).ToImmutableArray();
        return (new(produced.EventContract, values) { EventSource = identity, Route = _commandRoute }, destination);
    }

    static string CanonicalArray(SemanticArrayValue array, SemanticTypeReference type, SemanticApplicationContext context) =>
        $"[{string.Join(',', array.Values.Select(element => Canonical(element, type with { IsCollection = false, IsOptional = false }, context)))}]";

    static string CanonicalComposite(SemanticCompositeValue composite, SemanticTypeReference type, SemanticApplicationContext context)
    {
        var properties = context.Types[type.Target].Properties.ToDictionary(property => property.Id);

        return $"{{{string.Join(',', composite.Properties.Select(member => $"{JsonSerializer.Serialize(properties[member.TargetProperty].Name)}:{Canonical(member.Value, properties[member.TargetProperty].Type, context)}"))}}}";
    }

    static string StorageKey(SemanticValue destination) => destination switch
    {
        // Chronicle requires a storage key even when the semantic direct append has no identity.
        // This private log key never becomes the fact's modeled destination or typed context.
        SemanticNullValue => "stage:semantic-null",
        SemanticTextValue text => text.Value,
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticBooleanValue boolean => boolean.Value.ToString(CultureInfo.InvariantCulture),
        _ => throw new UnsupportedSemanticMapping()
    };

    Dictionary<SemanticId, SemanticValue> Inputs() => specification.When!.Values.Concat(specification.When.GeneratedValues).ToDictionary(value => value.TargetProperty, value => value.Value);
}
