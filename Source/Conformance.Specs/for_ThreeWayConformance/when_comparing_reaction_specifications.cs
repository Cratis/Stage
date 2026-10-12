// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

// The rendered leg remains refused until reaction specification rendering is delivered separately.
public class when_comparing_reaction_specifications : Specification
{
    [Theory]
    [InlineData("identity")]
    [InlineData("filters")]
    [InlineData("reactions")]
    public async Task should_match_the_reference_for_the_canonical_event_subset(string form)
    {
        var vector = form switch
        {
            "identity" => ReactionIdentityCorpus.V10,
            "filters" => EventRoutesCorpus.ObserverFiltersV10,
            _ => ReactionsCorpus.V6
        };
        var model = SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan());
        if (form != "identity") model = EventSubset(model, form);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var admitted = plan.Specifications.Values.Where(specification => SemanticSpecificationAdmission.Check(plan, specification) is null).ToArray();
        admitted.ShouldNotBeEmpty();
        foreach (var specification in admitted)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
            Assert.True(reference.Passed, $"{specification.Name}: {string.Join(';', reference.Failures)}");
            Assert.True(stage.Outcome == SemanticSpecificationOutcome.Passed, $"{specification.Name}: {stage.Unsupported?.Details} {string.Join(';', stage.Failures)}");
            stage.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
            if (reference.Execution is SemanticAccepted accepted)
            {
                Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), stage.Trace!.Facts.Select(fact => fact.EventContract));
                Assert.Equal(accepted.Facts.Select(fact => fact.Route), stage.Trace.Facts.Select(fact => fact.Route));
            }
            if (reference.Execution is SemanticRejected rejected) stage.Trace!.Rejection.ShouldEqual(rejected.Details);
        }
    }

    [Theory]
    [InlineData("", "amount > 0", true)]
    [InlineData("", "amount > 1", false)]
    [InlineData("runs as system", "amount > 0", true)]
    public async Task should_match_inline_productions_and_invocations(string identity, string condition, bool fires)
    {
        var source = $"""
            module Work
              feature Items
                slice Automation Process
                  command Complete
                    item String identifier
                    produces Completed
                      for item
                  event Started
                    item String
                    amount Decimal
                  event Followed
                    item String
                  event Completed
                  reaction Worker
                    {identity}
                    when Started
                      item
                      amount
                      produces Followed
                        item = item
                      invokes Complete
                        item = item
                    where {condition}
                  specification Exercising
                    when append Started
                      item = "one"
                      amount = 1
                    then Followed
                      item = "one"
                    then Completed
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InlineReactions"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("reaction"), "reaction", "Reaction.play", source);
        var compilation = new SemanticModelCompiler().Compile("InlineReactions", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        reference.Passed.ShouldEqual(fires);
        stage.Outcome.ShouldEqual(fires ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed);
        stage.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
        Assert.Equal(((SemanticAccepted)reference.Execution).Facts.Select(fact => fact.EventContract.ToString()), stage.Trace!.Facts.Select(fact => fact.EventContract));
    }

    static ExecutableSemanticModel EventSubset(ExecutableSemanticModel model, string form)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Where(slice => slice.Kind != SemanticSliceKind.Translate).Select(slice => slice with
        {
            Reducers = [],
            Reactions = [.. slice.Reactions.Where(reaction => reaction.Triggers.All(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.RequirementId is null))],
            Specifications = [.. slice.Specifications.Where(specification => form == "reactions" ? specification.WhenTrigger is null && specification.WhenClock is null && specification.WhenCapture is null : specification.Name.EndsWith("Reaction", StringComparison.Ordinal))]
        });

        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Triggers = [],
            Modules = [module with { Features = [feature with { Slices = [.. slices] }] }]
        });
    }
}
