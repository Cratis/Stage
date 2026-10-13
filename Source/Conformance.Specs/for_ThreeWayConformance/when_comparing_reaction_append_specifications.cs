// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_reaction_append_specifications(when_comparing_reaction_append_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_reaction_append_specifications.context>
{
    [Fact] void should_compare_guarded_productions_rejections_authentication_and_routes() => fixture.Outcomes.Length.ShouldEqual(6);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_every_rendered_specification() => fixture.Outcomes.All(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_three_way_application
    {
        internal const string Source = """
            policy CanComplete
              require authenticated
            eventsource WorkSource
              id "Work"
              identifier String
              stream Notes
                id "Notes"
                streamId String
            module Work
              feature Processing
                slice Automation FollowUp
                  command Complete
                    id String identifier
                    amount Decimal
                    authorize CanComplete
                    validate
                      amount > 0 message "A positive amount is required"
                    produces Completed
                      for id
                      amount = amount
                  event Started
                    id String
                    amount Decimal
                  event Requested
                    id String
                    amount Decimal
                  event Burst
                    id String
                    amount Decimal
                  event Completed
                    amount Decimal
                  event Followed
                    amount Decimal
                  event Deferred
                    amount Decimal
                  reaction ProducePositive
                    where amount > 0
                    when Started
                      produces Followed
                        for id
                        amount = amount
                  reaction ProduceNonpositive
                    where amount <= 0
                    when Started
                      produces Deferred
                        for id
                        amount = amount
                  reaction Invoke
                    runs as system
                    when Requested
                      invokes Complete
                        id = id
                        amount = amount
                  reaction ProduceBurst
                    when Burst
                      produces Followed
                        for id
                        amount = amount
                      produces Followed
                        for "another"
                        amount = 3
                  specification Production
                    when append Started
                      id = "one"
                      amount = 5
                    then Followed
                      amount = 5
                  specification NonpositiveProduction
                    when append Started
                      id = "zero"
                      amount = 0
                    then Deferred
                      amount = 0
                  specification ValidationRejected
                    when append Requested
                      id = "invalid"
                      amount = 0
                    then error "A positive amount is required"
                  specification AuthenticatedWithoutRoles
                    when append Requested
                      id = "valid"
                      amount = 5
                    then Completed
                      amount = 5
                  specification RoutedAppend
                    when append Started
                      for "routed"
                      stream WorkSource.Notes
                        streamId = "notes"
                      id = "routed"
                      amount = 5
                    then Followed
                      no stream
                      amount = 5
                  specification UnorderedBurst
                    when append Burst
                      id = "burst"
                      amount = 5
                    then Followed
                      amount = 3
                    then Followed
                      amount = 5
            """;

        protected override string ApplicationName => "ReactionAppends";
        protected override string ProjectFile => "ReactionAppends.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("inline", CompileModel())];
        Task Because() => Verify();

        static ExecutableSemanticModel CompileModel()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReactionAppends"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("appends"), "appends", "Appends.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ReactionAppends", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            var model = compilation.Value!.Model;
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single();
            var specifications = slice.Specifications.Select(specification => specification.Name == "UnorderedBurst"
                ? specification with { ThenEventsInAnyOrder = true } : specification);

            return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [.. specifications] }] }] }]
            });
        }
    }
}
#endif
