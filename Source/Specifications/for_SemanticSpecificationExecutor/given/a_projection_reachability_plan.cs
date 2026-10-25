// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_projection_reachability_plan : Specification
{
    protected static SemanticExecutionPlan Compile(string observation = "unrelated", string expected = "recorded")
    {
        var root = observation == "from" ? "Observed" : "OtherObserved";
        var extra = observation switch
        {
            "removal" => "        remove with Observed",
            "all" => "        all\n          id = $eventSourceId",
            "every" => "        every\n          exclude children\n          id = $eventSourceId",
            _ => string.Empty
        };
        var fixture = observation == "seed" ? "        given readmodel OtherView\n          id = \"one\"\n          count = 0" : string.Empty;
        var assertion = observation == "assertion" ? "        then readmodel OtherView\n          id = \"one\"\n          count = 3" : string.Empty;
        var source = $$"""
            module Work
              feature Work
                slice StateChange Recording
                  event Observed
                    name String
                  event OtherObserved
                    name String
                  command Record
                    id String identifier
                    name String
                    produces Observed
                      for id
                      name = name
                  command RecordOther
                    id String identifier
                    name String
                    produces OtherObserved
                      for id
                      name = name
                  specification Recording
            {{fixture}}
                    when Record
                      id = "one"
                      name = "recorded"
                    then Observed
                      for "one"
                      name = "recorded"
                    then readmodel Current
                      id = "one"
                      name = "{{expected}}"
            {{assertion}}
                slice StateView Current
                  readmodel Current
                    id String key
                    name String
                  projection CurrentProjection => Current
                    no automap
                    from Observed
                      id = $eventSourceId
                      name = name
                slice StateView Other
                  readmodel OtherView
                    id String key
                    count Int
                  projection UnadmittableProjection => OtherView
                    no automap
                    from {{root}}
                      id = $eventSourceId
                      add count by 3
            {{extra}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Reachability"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Reachability", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        if (observation == "flat")
        {
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single(candidate => candidate.Name == "Other");
            var projection = slice.Projections.Single();
            var @event = feature.Slices.SelectMany(candidate => candidate.Events).Single(candidate => candidate.Name == "Observed");
            var property = slice.ReadModels.Single().Properties.Single(candidate => candidate.Name == "count");
            var key = new SemanticResolvedExpression(SemanticExpressionRootKind.Event, SemanticExpressionSourceKind.Property, @event.Properties.Single().Id);
            var transition = new SemanticProjectionTransition(@event.Id, new(AffectedInstanceCardinality.One, key), [new(property.Id, new SemanticValueExpression(SemanticValue.Number(3)))]);
            var application = model.Application with { Modules = [module with { Features = [feature with
            {
                Slices = [.. feature.Slices.Select(candidate => candidate.Id != slice.Id ? candidate : slice with
                { Projections = [projection with { Scope = null, Transitions = [transition] }] })]
            }] }] };
            model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        }
        return SemanticExecutionPlan.Compile(model).Plan!;
    }
}
