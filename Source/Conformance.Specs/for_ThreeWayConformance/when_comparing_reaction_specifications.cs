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

// Executor parity also covers filters and mixed effects that remain outside renderer admission.
// The unmodified identity corpus has a separate three-way generated-application specification.
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

        // WrongStreamReaction and UnroutedReaction assert only read-model absence in the refreshed corpus.
        // Removing those outcomes leaves no event behavior to compare in this subset.
        if (form == "filters") admitted.Select(specification => specification.Name).ShouldContainOnly(["RoutedReaction"]);
        foreach (var specification in admitted)
        {
            await AssertMatchesReference(plan, specification);
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

    static async Task AssertMatchesReference(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        Assert.True(reference.Passed, $"{specification.Name}: {string.Join(';', reference.Failures)}");
        Assert.True(stage.Outcome == SemanticSpecificationOutcome.Passed, $"{specification.Name}: {stage.Unsupported?.Details} {string.Join(';', stage.Failures)}");
        stage.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
        switch (reference.Execution)
        {
            case SemanticAccepted accepted:
                Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), stage.Trace!.Facts.Select(fact => fact.EventContract));
                Assert.Equal(accepted.Facts.Select(fact => fact.Route), stage.Trace.Facts.Select(fact => fact.Route));
                break;
            case SemanticRejected rejected:
                stage.Trace!.Rejection.ShouldEqual(rejected.Details);
                break;
        }
    }

    static ExecutableSemanticModel EventSubset(ExecutableSemanticModel model, string form)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Where(slice => slice.Kind != SemanticSliceKind.Translate).Select(slice => slice with
        {
            Reducers = [],

            // The filters subset compares event effects, not the refreshed Followups projection.
            ReadModels = form == "filters" ? [] : slice.ReadModels,
            Projections = form == "filters" ? [] : slice.Projections,
            Queries = form == "filters" ? [] : slice.Queries,
            Reactions = [.. slice.Reactions.Where(reaction => reaction.Triggers.All(trigger => trigger.RequirementId is null))],
            Specifications = [.. slice.Specifications
                .Where(specification => form == "reactions" ? specification.WhenCapture is null && specification.GivenCaptures.IsDefaultOrEmpty && specification.Name != "UnsupportedStartup" : specification.Name.EndsWith("Reaction", StringComparison.Ordinal))
                .Select(specification => form == "filters" ? specification with
                {
                    // The refreshed corpus also asserts reducer-built Followups. This subset compares event effects only.
                    GivenReadModels = [],
                    ThenReadModels = [],
                    ThenAbsentReadModels = [],
                    ThenQueries = []
                } : specification)
                .Where(specification => form != "filters" || !specification.ThenEvents.IsEmpty)]
        });

        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Triggers = form == "reactions" ? model.Application.Triggers : [],
            Modules = [module with { Features = [feature with { Slices = [.. slices] }] }]
        });
    }
}
