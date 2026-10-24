// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;

public class a_projected_reaction : a_reaction_plan
{
    protected void CompileProjected(string payload = "item", string expected = "then readmodel Marker\n  item = \"one\"", string shape = "flat", string destination = "", string? reaction = null, ReactionScenario? scenario = null)
    {
        Compile(reaction ?? $"reaction Worker\n  when Started\n    item\n    produces Followed\n{destination}\n      item = {payload}", expected, scenario);
        var module = _plan.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var projection = slice.Projections.Single();
        var @event = slice.Events.Single(contract => contract.Name == "Followed");
        var model = slice.ReadModels.Single();
        var key = new SemanticResolvedExpression(SemanticExpressionRootKind.Event, SemanticExpressionSourceKind.Property, @event.Properties.Single().Id);
        var transition = new SemanticProjectionTransition(@event.Id, new(AffectedInstanceCardinality.One, key), [new(model.Properties.Single().Id, key)]);
        var flat = projection with { Scope = null, Transitions = [transition] };
        var projections = shape switch
        {
            "literal" => new[] { flat with { Transitions = [transition with { Mappings = [new(model.Properties.Single().Id, new SemanticValueExpression(SemanticValue.Text("one")))] }] } },
            "duplicate" => [flat, flat with { Id = SemanticId.Parse("sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Name = "OtherMarkers" }],
            "scoped" => [projection with { Scope = projection.Scope! with { From = [projection.Scope!.From.Single() with { EventContract = @event.Id }] } }],
            _ => [flat]
        };
        var application = _plan.Model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Projections = [.. projections] }] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, application)).Plan!;
    }
}
