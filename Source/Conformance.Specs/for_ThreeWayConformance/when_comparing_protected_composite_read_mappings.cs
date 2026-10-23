// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Proves that a command mapping a collection and a composite from a protected read into its event, through a projection that sets both whole,
/// executes identically on the reference evaluator and the rendered application, including a missing read. Stage executes all three, including the two whole composite and collection copies.
/// </summary>
/// <param name="fixture">The three-way fixture.</param>
/// <param name="output">The test output.</param>
public class when_comparing_protected_composite_read_mappings(when_comparing_protected_composite_read_mappings.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_protected_composite_read_mappings.context>
{
    const int SpecificationCount = 3;

    [Fact] void should_compare_every_specification() => fixture.Outcomes.Length.ShouldEqual(SpecificationCount);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_the_reference_and_the_rendered_application() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed", StringComparison.Ordinal) && outcome.Contains("Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(SpecificationCount);
    [Fact] void should_execute_the_structured_projection_and_the_missing_read() => Assert.Equal(
        [
            "source/ConfirmingAPlacedOrder: Reference=Passed, Stage=Passed, Rendered=Passed",
            "source/ConfirmingAnOrderWithoutLines: Reference=Passed, Stage=Passed, Rendered=Passed",
            "source/ConfirmingAnUnknownOrder: Reference=Passed, Stage=Passed, Rendered=Passed"
        ],
        fixture.Outcomes.Order(StringComparer.Ordinal));

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            type Address
              street String
              city String
            type Line
              sku String
              quantity Int
            module Orders
              feature Confirming
                slice StateChange Placing
                  event OrderPlaced
                    shipTo Address
                    lines Line[]
                  command PlaceOrder
                    id String identifier
                    shipTo Address
                    lines Line[]
                    produces OrderPlaced
                      for id
                      shipTo = shipTo
                      lines = lines
                  event OrderConfirmed
                    shipTo Address
                    lines Line[]
                  command ConfirmOrder
                    id String identifier
                    reads PlacedOrder by id
                    produces OrderConfirmed
                      for id
                      shipTo = PlacedOrder.shipTo
                      lines = PlacedOrder.lines
                  specification ConfirmingAPlacedOrder
                    given OrderPlaced
                      for "order"
                      shipTo = {"street":"1 Way","city":"Oslo"}
                      lines = [{"sku":"A","quantity":2},{"sku":"B","quantity":1}]
                    when ConfirmOrder
                      id = "order"
                    then OrderConfirmed
                      for "order"
                      shipTo = {"street":"1 Way","city":"Oslo"}
                      lines = [{"sku":"A","quantity":2},{"sku":"B","quantity":1}]
                  specification ConfirmingAnOrderWithoutLines
                    given OrderPlaced
                      for "order"
                      shipTo = {"street":"1 Way","city":"Oslo"}
                      lines = []
                    when ConfirmOrder
                      id = "order"
                    then OrderConfirmed
                      for "order"
                      shipTo = {"street":"1 Way","city":"Oslo"}
                      lines = []
                  specification ConfirmingAnUnknownOrder
                    when ConfirmOrder
                      id = "unknown"
                    then error "PlacedOrder 'unknown' does not exist."
                slice StateChange Cancelling
                  event OrderCancelled
                  command CancelOrder
                    id String identifier
                    produces OrderCancelled
                      for id
                slice StateView Placed
                  readmodel PlacedOrder
                    id String key
                    shipTo Address
                    lines Line[]
                  projection BuildPlaced => PlacedOrder
                    from OrderPlaced
                      id = $eventSourceId
                      shipTo = shipTo
                      lines = lines
                    remove with OrderCancelled
                  query PlacedById => PlacedOrder optional
                    by id String
            """;

        protected override string ApplicationName => "CompositeReadMappings";
        protected override string ProjectFile => "CompositeReadMappings.csproj";
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeReadMappings"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("CompositeReadMappings", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
