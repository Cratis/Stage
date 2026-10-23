// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_reaction_collection_specifications(when_comparing_reaction_collection_specifications.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_reaction_collection_specifications.context>
{
    [Fact] void should_compare_both_production_and_invocation_collections() => fixture.Outcomes.Length.ShouldEqual(2);
    [Fact] void should_match_the_reference_and_rendered_outcomes() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_match_all_three_collection_outcomes() => Assert.True(fixture.Outcomes.All(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)), string.Join(Environment.NewLine, fixture.Outcomes));

    public class context : a_three_way_application
    {
        const string Source = """
            module Work
              feature Processing
                slice Automation FollowUp
                  command CompleteLabels
                    id String identifier
                    labels String[]
                    produces LabelsCompleted
                      for id
                      labels = labels
                  event LabelsStarted
                    id String
                    labels String[]
                    alternateLabels String[]
                  event LabelsRequested
                    id String
                    labels String[]
                  event LabelsFollowed
                    labels String[]
                  event LabelsCompleted
                    labels String[]
                  reaction ProduceLabels
                    when LabelsStarted
                      produces LabelsFollowed
                        for id
                        labels = labels
                      produces LabelsFollowed
                        for "another"
                        labels = alternateLabels
                  reaction InvokeLabels
                    when LabelsRequested
                      invokes CompleteLabels
                        id = id
                        labels = labels
                  specification CollectionProduction
                    when append LabelsStarted
                      id = "labels"
                      labels = ["first", "second"]
                      alternateLabels = ["third"]
                    then LabelsFollowed
                      labels = ["third"]
                    then LabelsFollowed
                      labels = ["first", "second"]
                  specification CollectionInvocation
                    when append LabelsRequested
                      id = "labels"
                      labels = ["first", "second"]
                    then LabelsCompleted
                      labels = ["first", "second"]
            """;

        protected override string ApplicationName => "ReactionCollections";
        protected override string ProjectFile => "ReactionCollections.csproj";

        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("inline", CompileModel())];
        Task Because() => Verify();

        static ExecutableSemanticModel CompileModel()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReactionCollections"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("collections"), "collections", "Collections.play", Source);
            var compilation = new SemanticModelCompiler().Compile("ReactionCollections", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            var model = compilation.Value!.Model;
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single();
            var specifications = slice.Specifications.Select(specification => specification.Name == "CollectionProduction"
                ? specification with { ThenEventsInAnyOrder = true } : specification);

            return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [.. specifications] }] }] }]
            });
        }
    }
}
#endif
