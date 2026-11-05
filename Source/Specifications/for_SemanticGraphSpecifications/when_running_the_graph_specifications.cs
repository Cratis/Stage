// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticGraphSpecifications;

public class when_running_the_graph_specifications : Specification
{
    const string Source = """
        concept PersonId   : Uuid
        concept GroupId    : Uuid
        concept PersonName : String
        concept GroupName  : String

        module Organization
          feature Chart
            slice StateView OrganizationChart
              event PersonRegistered
                name PersonName
              event PersonRemoved
              event GroupCreated
                name GroupName
              event MemberAdded
                groupId GroupId

              graph Organization
                node Person
                  personId PersonId key
                  name     PersonName
                node Group
                  groupId GroupId key
                  name    GroupName
                edge MEMBER_OF source Person target Group

              projection OrganizationChart => graph Organization
                node Person
                  from PersonRegistered key $eventSourceId
                    personId = $eventSourceId
                    name     = name
                  remove with PersonRemoved key $eventSourceId detach
                node Group
                  from GroupCreated key $eventSourceId
                    groupId = $eventSourceId
                    name    = name
                edge MEMBER_OF
                  from MemberAdded source $eventSourceId target groupId

              specification AddingAMemberDrawsAnEdge
                given PersonRegistered
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  name = "Ada Lovelace"
                given GroupCreated
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "Engineering"
                when append MemberAdded
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  groupId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                then graph Organization
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                    name = "Engineering"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"

              specification RemovingAPersonDetachesTheirEdges
                given graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                    name = "Ada Lovelace"
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
                    name = "Engineering"
                  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
                when append PersonRemoved
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                then graph Organization
                  no node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  no edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
                  has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"

              specification AMemberWhoWasNeverAddedHasNoEdge
                given PersonRegistered
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                  name = "Ada Lovelace"
                when append PersonRemoved
                  for "6fa459ea-ee8a-4ca4-894e-db77e160355e"
                then graph Organization
                  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
        """;

    SemanticSpecificationRunReport _all = null!;
    SemanticSpecificationRunReport _sliceScoped = null!;
    SemanticSpecificationRunReport _otherScoped = null!;
    SemanticExecutionPlan _plan = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Organization"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("organization"), "organization", "Organization.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Organization", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        var compiled = SemanticExecutionPlan.Compile(compilation.Value.Model);
        Assert.True(compiled.Success, string.Join("; ", compiled.Issues));
        _plan = compiled.Plan;
    }

    async Task Because()
    {
        var executor = new SemanticSpecificationExecutor();
        var slice = _plan.Model.Application.Modules[0].Features[0].Slices[0];
        _all = await executor.Run(_plan, SemanticSpecificationSelection.All, new());
        _sliceScoped = await executor.Run(_plan, new([slice.Id]), new());
        _otherScoped = await executor.Run(_plan, new([_plan.Model.Application.Modules[0].Id]), new());
    }

    IEnumerable<SemanticSpecificationRunRecord> Graphs(SemanticSpecificationRunReport report) => report.Results.Where(result => result.SliceKind == "Graph");

    [Fact] void should_run_every_graph_specification() => Graphs(_all).Select(result => result.Name).ShouldContainOnly(["AddingAMemberDrawsAnEdge", "RemovingAPersonDetachesTheirEdges", "AMemberWhoWasNeverAddedHasNoEdge"]);
    [Fact] void should_pass_the_ones_that_hold() => Graphs(_all).Where(result => result.Name != "AMemberWhoWasNeverAddedHasNoEdge").ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    [Fact] void should_run_a_specification_that_seeds_a_graph() => Graphs(_all).Single(result => result.Name == "RemovingAPersonDetachesTheirEdges").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_fail_the_one_that_asserts_what_the_graph_does_not_hold() => Graphs(_all).Single(result => result.Name == "AMemberWhoWasNeverAddedHasNoEdge").Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_say_what_did_not_hold() => Graphs(_all).Single(result => result.Name == "AMemberWhoWasNeverAddedHasNoEdge").Failures.ShouldNotBeEmpty();
    [Fact] void should_run_them_with_the_slice_that_owns_them() => Graphs(_sliceScoped).Count().ShouldEqual(3);
    [Fact] void should_run_them_with_the_module_that_contains_them() => Graphs(_otherScoped).Count().ShouldEqual(3);
    [Fact] void should_report_the_owning_slice() => Graphs(_all).ShouldEachConformTo(result => result.SliceId == _plan.Model.Application.Modules[0].Features[0].Slices[0].Id.ToString());
}
