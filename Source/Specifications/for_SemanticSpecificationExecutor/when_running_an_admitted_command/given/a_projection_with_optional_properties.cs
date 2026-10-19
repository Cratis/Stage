// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command.given;

public class a_projection_with_optional_properties : a_command_only_plan
{
    protected virtual bool MapValues => false;

    protected virtual bool OnlyOptional => false;

    protected void Configure()
    {
        var application = _originalModel.Application;
        var slices = application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).ToArray();
        var readModel = slices.SelectMany(slice => slice.ReadModels).Single();
        var projection = slices.SelectMany(slice => slice.Projections).Single();
        var fact = _original.ThenEvents.Single();
        var identifier = readModel.Properties.Single(property => property.IsIdentifier);
        var source = new SemanticEventSourceIdentity(identifier.Type, _original.ThenReadModels.Single().Key);
        var types = new[] { SemanticPrimitiveType.WholeNumber, SemanticPrimitiveType.DecimalNumber, SemanticPrimitiveType.Boolean, SemanticPrimitiveType.Uuid, SemanticPrimitiveType.Date, SemanticPrimitiveType.Text };
        SemanticValue[] values = [SemanticValue.Number(42), SemanticValue.Number(12.5m), SemanticValue.Boolean(true), SemanticValue.Text("12345678-1234-1234-1234-123456789abc"), SemanticValue.Text("2026-10-18"), SemanticValue.Text("Optional text")];
        var properties = types.Select((type, index) => new SemanticProperty(
            SemanticId.Parse($"sem1:{index + 1000:x64}"), $"optional{type}", SemanticTypeReference.ForPrimitive(type) with { IsOptional = true }, false)).ToArray();
        var eventProperties = properties.Select((property, index) => property with { Id = SemanticId.Parse($"sem1:{index + 2000:x64}"), Type = property.Type with { IsOptional = false } }).Take(MapValues ? properties.Length : 0).ToArray();
        var kept = readModel.Properties.Where(property => !OnlyOptional || property.IsIdentifier).ToArray();
        var keptIds = kept.Select(property => property.Id).ToHashSet();
        var updatedModel = readModel with
        {
            Properties = [.. kept, .. properties]
        };
        var assertion = _original.ThenReadModels.Single() with
        {
            Values = [.. _original.ThenReadModels.Single().Values.Where(value => keptIds.Contains(value.TargetProperty)), .. properties.Select((property, index) => new SemanticPropertyValue(property.Id, MapValues ? values[index] : SemanticValue.Null))]
        };
        var updatedProjection = projection with
        {
            Scope = projection.Scope! with
            {
                From = [.. projection.Scope!.From.Select(root => root with
                {
                    Mappings = [.. root.Mappings.Where(_ => !OnlyOptional), .. eventProperties.Select((property, index) => new SemanticProjectionMapping([properties[index].Id], SemanticProjectionOperation.Set, new SemanticProjectionEventProperty([property.Id])))]
                })]
            }
        };
        _specification = _original with
        {
            When = null,
            WhenAppended = new(fact.EventContract, [.. fact.Values, .. eventProperties.Select((property, index) => new SemanticPropertyValue(property.Id, values[index]))]) { EventSource = source },
            ThenEvents = [],
            ThenReadModels = [assertion],
            ThenQueries = []
        };
        var changed = application with
        {
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command with
                        {
                            Produces = [.. command.Produces.Select(produced => produced.EventContract == fact.EventContract ? produced with
                            {
                                Mappings = [.. produced.Mappings, .. eventProperties.Select((property, index) => new SemanticPropertyMapping(property.Id, new SemanticValueExpression(values[index])))]
                            } : produced)]
                        })],
                        Constraints = [],
                        Queries = [],
                        Events = [.. slice.Events.Select(contract => contract.Id == fact.EventContract ? contract with { Properties = [.. contract.Properties, .. eventProperties] } : contract)],
                        ReadModels = [.. slice.ReadModels.Select(model => model.Id == readModel.Id ? updatedModel : model)],
                        Projections = [.. slice.Projections.Select(candidate => candidate.Id == projection.Id ? updatedProjection : candidate)],
                        Specifications = slice.Specifications.Any(specification => specification.Id == _original.Id) ? [_specification] : []
                    })]
                })]
            })]
        };
        var compilation = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_originalModel.LanguageVersion, _originalModel.SemanticVersion, changed));
        Assert.Empty(compilation.Issues);
        _plan = compilation.Plan!;
    }
}
