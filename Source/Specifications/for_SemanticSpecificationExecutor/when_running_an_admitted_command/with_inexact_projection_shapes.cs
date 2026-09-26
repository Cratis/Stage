// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_inexact_projection_shapes : a_command_only_plan
{
    readonly List<string> _failures = [];

    async Task Because()
    {
        var projection = _originalModel.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Projections).Single();
        var model = _originalModel.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.ReadModels).Single();
        var fact = _original.ThenEvents.Single();
        var identity = model.Properties.Single(property => property.IsIdentifier);
        var name = model.Properties.Single(property => property.Id != identity.Id);
        var @event = _originalModel.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Events).Single(candidate => candidate.Id == fact.EventContract);
        var eventName = @event.Properties.Single(property => property.Name == name.Name);
        var eventIdentity = new SemanticProperty(SemanticId.Parse("sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), identity.Name, identity.Type, false);
        var flatEvent = @event with { Properties = [.. @event.Properties, eventIdentity] };
        var key = new SemanticResolvedExpression(SemanticExpressionRootKind.Event, SemanticExpressionSourceKind.Property, eventIdentity.Id);
        var transition = new SemanticProjectionTransition(
            @event.Id,
            new(AffectedInstanceCardinality.One, key),
            [new(identity.Id, key), new(name.Id, new SemanticResolvedExpression(SemanticExpressionRootKind.Event, SemanticExpressionSourceKind.Property, eventName.Id))]);
        var flat = projection with { Scope = null, Transitions = [transition] };
        var source = fact.EventSource ?? new SemanticEventSourceIdentity(identity.Type, _original.When!.Values.Single(value => value.TargetProperty == _originalModel.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Commands).Single().Properties.Single(property => property.Name == identity.Name).Id).Value);
        var append = _original with { When = null, WhenAppended = new(fact.EventContract, fact.Values) { EventSource = source }, ThenEvents = [] };
        var flatAppend = append with
        {
            WhenAppended = new(@event.Id, [.. fact.Values, new SemanticPropertyValue(eventIdentity.Id, source.Value)]) { EventSource = source },
            ThenEvents = []
        };
        var root = new SemanticProjectionFrom(
            fact.EventContract,
            SemanticProjectionKey.EventSourceIdentity,
            null,
            [new([name.Id], SemanticProjectionOperation.Set, new SemanticProjectionEventProperty([eventName.Id]))]);
        var scoped = projection with { Scope = SemanticProjectionScope.Empty with { From = [root] }, Transitions = [] };
        await Check(
            "flat identifier omitted",
            flatAppend,
            flat with { Transitions = [transition with { Mappings = [transition.Mappings[1]] }] },
            false,
            SemanticSpecificationOutcome.Unsupported,
            eventContract: flatEvent);
        await Check(
            "flat scalar omitted",
            flatAppend,
            flat with { Transitions = [transition with { Mappings = [transition.Mappings[0]] }] },
            false,
            SemanticSpecificationOutcome.Unsupported,
            eventContract: flatEvent);
        await Check(
            "identifier-only empty from",
            append with { ThenReadModels = [], ThenQueries = [new(_original.ThenQueries.Single().Query, _original.ThenQueries.Single().Key, [])] },
            scoped with { Scope = scoped.Scope with { From = [root with { Mappings = [] }] } },
            false,
            SemanticSpecificationOutcome.Unsupported,
            model with { Properties = [identity] });
        var numeric = SemanticValue.Number(42);
        var numericModel = model with { Properties = [.. model.Properties.Select(property => property.Id == identity.Id ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) } : property)] };
        var numericAssertion = _original.ThenReadModels.Single() with
        {
            Key = SemanticValue.Text("42"),
            Values = [.. _original.ThenReadModels.Single().Values.Select(value => value.TargetProperty == identity.Id ? value with { Value = SemanticValue.Text("42") } : value)]
        };
        await Check(
            "numeric source / text identifier",
            append with { WhenAppended = new(fact.EventContract, fact.Values) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), numeric) }, ThenReadModels = [numericAssertion], ThenQueries = [] },
            scoped,
            false,
            SemanticSpecificationOutcome.Unsupported,
            numericModel,
            numericSource: true);
        var second = projection with { Id = SemanticId.Parse("sem1:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"), Name = "OtherProjection" };
        await Check("multiple projections", _original, projection, true, SemanticSpecificationOutcome.Unsupported, extra: second);

        // The reference keeps the original DateTime text, while a CLR round-trip would turn Z into +00:00.
        var timestamp = SemanticValue.Text("2025-01-01T00:00:00.0000000Z");
        var datedFact = fact with { Values = [.. fact.Values.Select(value => value.TargetProperty == eventName.Id ? value with { Value = timestamp } : value)] };
        var datedModel = model with { Properties = [.. model.Properties.Select(property => property.Id == name.Id ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime) } : property)] };
        var datedAssertion = _original.ThenReadModels.Single() with { Values = [.. _original.ThenReadModels.Single().Values.Select(value => value.TargetProperty == name.Id ? value with { Value = timestamp } : value)] };
        var datedEvent = @event with { Properties = [.. @event.Properties.Select(property => property.Id == eventName.Id ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime) } : property)] };
        await Check(
            "DateTime text",
            append with { WhenAppended = new(fact.EventContract, datedFact.Values) { EventSource = source }, ThenReadModels = [datedAssertion], ThenQueries = [] },
            scoped,
            true,
            SemanticSpecificationOutcome.Unsupported,
            datedModel,
            eventContract: datedEvent,
            dateTimeSource: true,
            rejectionContains: "DateTime");
    }

    [Fact] void should_reject_flat_transitions_without_an_identifier_mapping() => Verify("flat identifier omitted");
    [Fact] void should_reject_flat_transitions_without_a_required_scalar_mapping() => Verify("flat scalar omitted");
    [Fact] void should_reject_identifier_only_mapping_free_scopes() => Verify("identifier-only empty from");
    [Fact] void should_reject_number_sources_for_text_identifiers() => Verify("numeric source / text identifier");
    [Fact] void should_reject_multiple_projections_for_one_read_model() => Verify("multiple projections");
    [Fact] void should_reject_lossy_datetime_projections() => Verify("DateTime text");

    void Verify(string caseName) => Assert.DoesNotContain(_failures, failure => failure.StartsWith($"{caseName}:", StringComparison.Ordinal));

    async Task Check(
        string caseName,
        SemanticSpecification specification,
        SemanticProjection projection,
        bool referencePasses,
        SemanticSpecificationOutcome stageOutcome,
        SemanticReadModel? readModel = null,
        SemanticProjection? extra = null,
        SemanticEventContract? eventContract = null,
        bool numericSource = false,
        bool dateTimeSource = false,
        string? rejectionContains = null)
    {
        var application = _originalModel.Application;
        var changed = application with { Modules = [.. application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Select(slice => slice with
            {
                Projections = [.. slice.Projections.Select(candidate => candidate.Id == projection.Id ? projection : candidate), .. extra is null || !slice.Projections.Any(candidate => candidate.Id == projection.Id) ? [] : new[] { extra }],
                ReadModels = readModel is null ? slice.ReadModels : [.. slice.ReadModels.Select(candidate => candidate.Id == readModel.Id ? readModel : candidate)],
                Events = eventContract is null ? slice.Events : [.. slice.Events.Select(candidate => candidate.Id == eventContract.Id ? eventContract : candidate)],
                Commands = Commands(slice, eventContract, numericSource, dateTimeSource),
                Queries = numericSource ? [.. slice.Queries.Select(query => query with { Argument = query.Argument with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) } })] : slice.Queries,
                Specifications = [.. slice.Specifications.Where(candidate => (!numericSource && !dateTimeSource) || candidate.Id == specification.Id).Select(candidate => candidate.Id == specification.Id ? specification : candidate)]
            })] })]
        })] };
        var compilation = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_originalModel.LanguageVersion, _originalModel.SemanticVersion, changed));
        if (compilation.Plan is null)
        {
            _failures.Add($"{caseName}: model did not compile: {string.Join("; ", compilation.Issues)}");
            return;
        }
        var reference = new SemanticSpecificationRunner().Run(compilation.Plan, specification.Id);
        var stage = Assert.Single((await new SemanticSpecificationExecutor().Run(compilation.Plan, new([specification.Id]), new())).Results);
        if (reference.Passed != referencePasses || stage.Outcome != stageOutcome || stage.Unsupported?.Capability != StageExecutionCapability.Projection ||
            (rejectionContains is not null && stage.Unsupported?.Details.Contains(rejectionContains, StringComparison.Ordinal) != true))
        {
            _failures.Add($"{caseName}: reference {reference.Passed}/{reference.Execution.Kind} {string.Join("; ", reference.Failures)}; Stage {stage.Outcome}/{stage.Unsupported?.Capability}/{stage.Unsupported?.Details} {string.Join("; ", stage.Failures)}");
        }
    }

    static System.Collections.Immutable.ImmutableArray<SemanticCommand> Commands(SemanticSlice slice, SemanticEventContract? eventContract, bool numericSource, bool dateTimeSource)
    {
        if (numericSource)
        {
            return [.. slice.Commands.Select(command => command with
                {
                    Properties = [.. command.Properties.Select(property => property.IsIdentifier ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber) } : property)],
                    Destination = command.Destination is null ? null : command.Destination with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber) }
                })];
        }

        if (dateTimeSource)
        {
            return [.. slice.Commands.Select(command => command with
            {
                Properties = [.. command.Properties.Select(property => property.Name == eventContract!.Properties[0].Name ? property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime) } : property)],
                Validations = []
            })];
        }

        if (eventContract is null)
        {
            return slice.Commands;
        }

        return [.. slice.Commands.Select(command => command with
                {
                    Produces = [.. command.Produces.Select(produced => produced.EventContract == eventContract.Id ? produced with
                    {
                        Mappings = [.. produced.Mappings, new SemanticPropertyMapping(
                            eventContract.Properties[^1].Id,
                            new SemanticResolvedExpression(
                                SemanticExpressionRootKind.Command,
                                SemanticExpressionSourceKind.Property,
                                command.Properties.Single(property => property.Name == eventContract.Properties[^1].Name).Id))]
                    } : produced)]
                })];
    }
}
