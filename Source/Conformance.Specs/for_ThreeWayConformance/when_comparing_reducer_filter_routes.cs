// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_reducer_filter_routes(ITestOutputHelper output) : Specification
{
    [Theory]
    [InlineData("", false)]
    [InlineData("Account.Archive", false)]
    [InlineData("Other.Notes", false)]
    [InlineData("Account.Notes", true)]
    public async Task should_match_reference_for_an_isolated_reducer_without_a_projection(string route, bool refused)
    {
        var plan = Compile(route);
        plan.Projections.ShouldBeEmpty();
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        reference.Execution.Kind.ShouldEqual(refused ? SemanticExecutionOutcomeKind.Unsupported : SemanticExecutionOutcomeKind.Accepted);
        result.Outcome.ShouldEqual(refused ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed);
        if (refused) AssertMatchedRefusal(plan, reference, result);
        else reference.Passed.ShouldBeTrue();
        output.WriteLine($"Isolated route '{route}': PARITY; Reference={reference.Execution.Kind}; Stage={result.Outcome}");
    }

    static void AssertMatchedRefusal(SemanticExecutionPlan plan, SemanticSpecificationRun reference, SemanticSpecificationRunRecord result)
    {
        ((SemanticUnsupported)reference.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
        result.Unsupported.Construct.ShouldEqual(plan.ReadModels.Values.Single().Id.ToString());
        result.Unsupported.Details.ShouldContain("STAGE-IMPL-003:");
    }

    static SemanticExecutionPlan Compile(string route)
    {
        var source = $$"""
            eventsource Account
              stream Notes
              stream Archive
            eventsource Other
              stream Notes
            module M
              feature F
                slice Automation S
                  event Folded
                  command Fold
                    id String identifier
                    stream Account.Notes
                    produces Folded
                      for id
                  readmodel Snapshot
                    id String key
                  reducer Fold => Snapshot
                    from Account.Notes
                    on Folded
                      ```csharp
                      return context.State;
                      ```
                  specification Route
                    when append Folded
                      for "one"
                      {{(route.Length == 0 ? "" : $"stream {route}")}}
                    then no readmodel Snapshot for "one"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("IsolatedReducerRoutes"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("routes"), "routes", "routes.play", source);
        var compiled = new SemanticModelCompiler().Compile("IsolatedReducerRoutes", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        return SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
    }
}
