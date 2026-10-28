// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_projecting_key_only_read_models : Specification
{
    public enum ProjectionCase
    {
        MatchingEvent,
        WrongExpectedKey,
        UnrelatedEvent,
        FailedAbsence,
        EventPropertyKey,
        History,
        RepeatedEvent,
        RequiredProperty
    }

    static readonly IReadOnlyDictionary<ProjectionCase, string> Specifications = new Dictionary<ProjectionCase, string>
    {
        [ProjectionCase.MatchingEvent] = """
            when append Followed
              for "one"
            then readmodel Followups
              id = "one"
            """,
        [ProjectionCase.WrongExpectedKey] = """
            when append Followed
              for "one"
            then readmodel Followups
              id = "two"
            """,
        [ProjectionCase.UnrelatedEvent] = """
            when append Recorded
              for "one"
            then no readmodel Followups for "one"
            """,
        [ProjectionCase.FailedAbsence] = """
            when append Followed
              for "one"
            then no readmodel Followups for "one"
            """,
        [ProjectionCase.EventPropertyKey] = """
            when append Followed
              for "src"
              accountId = "acc"
            then readmodel Followups
              id = "acc"
            then no readmodel Followups for "src"
            """,
        [ProjectionCase.History] = """
            given Followed
              for "a"
            when append Followed
              for "b"
            then readmodel Followups
              id = "a"
            then readmodel Followups
              id = "b"
            """,
        [ProjectionCase.RepeatedEvent] = """
            given Followed
              for "one"
            when append Followed
              for "one"
            then readmodel Followups
              id = "one"
            """,
        [ProjectionCase.RequiredProperty] = """
            when append Followed
              for "one"
            then readmodel Followups
              id = "one"
              note = "missing"
            """
    };

    [Theory]
    [InlineData(ProjectionCase.MatchingEvent, SemanticSpecificationOutcome.Passed)]
    [InlineData(ProjectionCase.WrongExpectedKey, SemanticSpecificationOutcome.Failed)]
    [InlineData(ProjectionCase.UnrelatedEvent, SemanticSpecificationOutcome.Passed)]
    [InlineData(ProjectionCase.FailedAbsence, SemanticSpecificationOutcome.Failed)]
    public Task should_check_creation_and_absence(ProjectionCase scenario, SemanticSpecificationOutcome outcome) => AssertParity(Compile(scenario), outcome);

    [Theory]
    [InlineData(ProjectionCase.History)]
    [InlineData(ProjectionCase.RepeatedEvent)]
    public Task should_initialize_from_history_and_replay_repeated_events(ProjectionCase scenario) => AssertParity(Compile(scenario), SemanticSpecificationOutcome.Passed);

    [Fact]
    public async Task should_preserve_reference_unsupported_parity_for_an_event_property_key()
    {
        var plan = Compile(ProjectionCase.EventPropertyKey);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        var unsupported = Assert.IsType<SemanticUnsupported>(reference.Execution);
        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Projection);
        unsupported.Details.ShouldEqual("Projection 'FollowupsProjection' did not establish required read-model property 'id'.");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    }

    [Fact]
    public async Task should_refuse_unestablished_required_properties()
    {
        var plan = Compile(ProjectionCase.RequiredProperty);
        var refusal = SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single());
        refusal.ShouldNotBeNull();
        refusal!.Details.ShouldContain("At least one from block must establish the required read-model properties.");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Details.ShouldContain("At least one from block must establish the required read-model properties.");
    }

    static async Task AssertParity(SemanticExecutionPlan plan, SemanticSpecificationOutcome outcome)
    {
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        Assert.True(reference.Passed == (outcome == SemanticSpecificationOutcome.Passed), $"{reference.Execution}: {string.Join(';', reference.Failures)}");
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == outcome, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    static SemanticExecutionPlan Compile(ProjectionCase scenario)
    {
        var eventPropertyKey = scenario == ProjectionCase.EventPropertyKey;
        var source = $$"""
            module Tracking
              feature Followups
                slice StateView Followups
                  event Followed
            {{(eventPropertyKey ? "        accountId String" : "")}}
                  event Recorded
                  command Follow
                    id String identifier
                    produces Followed
                      for id
            {{(eventPropertyKey ? "          accountId = \"acc\"" : "")}}
                  command Record
                    id String identifier
                    produces Recorded
                      for id
                  readmodel Followups
                    id String key
            {{(scenario == ProjectionCase.RequiredProperty ? "        note String" : "")}}
                  projection FollowupsProjection => Followups
                    from Followed{{(eventPropertyKey ? " key accountId" : "")}}
                  specification Projecting
            {{string.Join('\n', Specifications[scenario].Split('\n').Select(line => "        " + line))}}
            """;
        var syntax = new ScreenplayCompiler().Parse(source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("KeyOnlyReadModels"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var bound = new SemanticModelBinder().Bind("KeyOnlyReadModels", syntax.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        return SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
    }
}
