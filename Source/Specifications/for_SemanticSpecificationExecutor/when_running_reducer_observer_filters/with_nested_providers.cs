// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_reducer_observer_filters;

public class with_nested_providers : Specification
{
    [Theory]
    [InlineData(0, "none")]
    [InlineData(0, "source")]
    [InlineData(0, "stream")]
    [InlineData(1, "none")]
    [InlineData(1, "source")]
    [InlineData(1, "stream")]
    [InlineData(2, "none")]
    [InlineData(2, "source")]
    [InlineData(2, "stream")]
    public async Task should_admit_and_execute_absence_without_reaching_the_filtered_body(int depth, string route)
    {
        var plan = Plan(depth, route);
        var specification = plan.Specifications.Values.Single();
        Assert.Empty(specification.GivenReadModels);
        Assert.Equal(plan.ReadModels.Values.Single(model => model.Name == "Snapshot").Id, Assert.Single(specification.ThenAbsentReadModels).ReadModel);
        Assert.Null(SemanticRunAdmission.Check(plan, specification));
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.Equal(SemanticExecutionOutcomeKind.Accepted, reference.Execution.Kind);
        Assert.True(reference.Passed);
        var retainedFacts = new List<int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, count) => retainedFacts.Add(count) };
        var result = Assert.Single((await executor.Run(plan, new([specification.Id]), new())).Results);
        Assert.Equal(SemanticSpecificationOutcome.Passed, result.Outcome);
        Assert.Null(result.Unsupported);
        Assert.Empty(result.Trace!.ReadModels);
        Assert.Empty(result.Trace.Facts);
        Assert.Contains(1, retainedFacts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task should_still_refuse_an_absence_assertion_with_no_provider(int depth)
    {
        var plan = Plan(depth, "none", provider: false);
        var specification = plan.Specifications.Values.Single();
        var refusal = Assert.IsType<SemanticUnsupportedCapability>(SemanticRunAdmission.Check(plan, specification));
        Assert.Equal(StageExecutionCapability.Projection, refusal.Capability);
        Assert.Equal(plan.ReadModels.Values.Single(model => model.Name == "Unprovided").Id.ToString(), refusal.Construct);
        Assert.Equal("The asserted read model has no projection in this run.", refusal.Details);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, result.Outcome);
        Assert.Equal(refusal.Details, result.Unsupported!.Details);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task should_still_refuse_a_route_that_reaches_the_authored_body(int depth)
    {
        var plan = Plan(depth, "match");
        var specification = plan.Specifications.Values.Single();
        var refusal = Assert.IsType<SemanticUnsupportedCapability>(SemanticRunAdmission.Check(plan, specification));
        Assert.Equal(StageExecutionCapability.Projection, refusal.Capability);
        Assert.Contains("STAGE-IMPL-003: Reducer 'Fold'", refusal.Details, StringComparison.Ordinal);
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);
        Assert.Equal(SemanticSpecificationOutcome.Unsupported, result.Outcome);
        Assert.Equal(refusal.Details, result.Unsupported!.Details);
    }

    static SemanticExecutionPlan Plan(int depth, string route, bool provider = true)
    {
        var parents = string.Concat(Enumerable.Range(0, depth).Select(index => new string(' ', 2 + (index * 2)) + $"feature Outer{index}\n"));
        var source = Sources + parents + new string(' ', 2 + (depth * 2)) + "feature Items\n" +
            string.Join('\n', Slice.Split('\n').Select(line => new string(' ', 4 + (depth * 2)) + line));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NestedFilters"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compiled = new SemanticModelCompiler().Compile("NestedFilters", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        var model = compiled.Value!.Model;
        var application = model.Application with { Modules = [model.Application.Modules.Single() with { Features = [Change(model.Application.Modules.Single().Features.Single())] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
        Assert.True(plan.Success, string.Join(';', plan.Issues));
        return plan.Plan!;

        SemanticFeature Change(SemanticFeature feature) => feature with
        {
            Features = [.. feature.Features.Select(Change)],
            Slices = [.. feature.Slices.Select(slice => slice with
            {
                Specifications = [.. slice.Specifications.Select(specification => specification with
                {
                    GivenEvents = [specification.GivenEvents.Single() with { Route = Route(model.Application, route) }],
                    ThenAbsentReadModels = provider ? specification.ThenAbsentReadModels : [new(slice.ReadModels.Single(model => model.Name == "Unprovided").Id, SemanticValue.Text("one"))]
                })]
            })]
        };
    }

    static SemanticFixtureRoute? Route(SemanticApplication application, string route)
    {
        if (route == "none") return null;
        var source = application.EventSources.Single(source => source.Name == (route == "source" ? "Other" : "Account"));
        return new(source.Id, source.Streams.Single(stream => stream.Name == (route == "stream" ? "Archive" : "Notes")).Id);
    }

    const string Sources = "eventsource Account\n  stream Notes\n  stream Archive\neventsource Other\n  stream Notes\nmodule Work\n";

    const string Slice = """
        slice StateChange Manage
          command Feed
            id String identifier
            stream Account.Notes
            produces Changed
              for id
          event Changed
          readmodel Snapshot
            id String key
          readmodel Unprovided
            id String key
          reducer Fold => Snapshot
            from Account.Notes
            on Changed
              ```csharp
              throw new InvalidOperationException("Never execute an authored body");
              ```
          specification Check
            given Changed
              for "one"
            then no readmodel Snapshot for "one"
        """;
}
