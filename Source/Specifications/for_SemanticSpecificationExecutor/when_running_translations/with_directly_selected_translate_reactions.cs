// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_translations;

public class with_directly_selected_translate_reactions : when_settling_reactions.given.a_reaction_plan
{
    // Screenplay decision 0115: a specification runs only its own slice, so a clock or trigger does not fire another slice's Translate
    // reaction; only an explicit redelivery to that reaction still selects it, and Stage refuses it.
    [Theory]
    [InlineData("startup")]
    [InlineData("application")]
    [InlineData("interval")]
    [InlineData("schedule")]
    public async Task should_not_fire_the_other_slices_translate_reaction(string action)
    {
        var result = await Run(action, "then no events");
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        result.Unsupported.ShouldBeNull();
    }

    [Fact]
    public async Task should_refuse_an_explicit_redelivery_to_the_other_slices_translate_reaction()
    {
        var result = await Run("redelivery", "then Completed\n  for \"one\"");
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Details.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
    }

    async Task<SemanticSpecificationRunRecord> Run(string action, string expectation)
    {
        var trigger = action switch
        {
            "startup" => "when Startup",
            "application" => "when Arrived",
            "interval" => "every 1 hour",
            "schedule" => "at 01:00",
            _ => "when Started"
        };
        var fixture = action switch
        {
            "startup" => "when trigger Startup",
            "application" => "when trigger Arrived",
            "redelivery" => "given Started\n  for \"one\"\n  item = \"one\"\n  amount = 1\nwhen redelivered Started to Worker",
            _ => "given clock \"2026-10-05T00:00:00Z\"\nwhen clock \"2026-10-05T01:00:00Z\""
        };
        Compile($"reaction Worker\n  {trigger}\n    produces Completed\n      for \"one\"", expectation, new(Action: fixture, Policy: action == "application" ? "trigger Arrived" : ""));
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var translated = slice with
        {
            Id = SemanticId.Parse("sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Name = "Translated",
            Kind = SemanticSliceKind.Translate,
            Commands = [], Events = [], ReadModels = [], Projections = [], Specifications = []
        };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Reactions = [] }, translated] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        return (await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results.Single();
    }
}
