// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_generation_rule_bodies(when_comparing_generation_rule_bodies.context fixture) : IClassFixture<when_comparing_generation_rule_bodies.context>
{
    [Fact] void should_pass_the_corpus_specification_on_all_three_paths() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_have_no_three_way_differences() => Assert.True(fixture.Differences.IsEmpty, string.Join(Environment.NewLine, fixture.Differences));

    public class context : a_three_way_application
    {
        static readonly LoadedSemanticModel[] _forms = [.. new[] { "single", "relocated" }.Select(form =>
            generation_rule_corpus.Compile(generation_rule_model.PortableBody, form, ruleOnly: true))];

        protected override string ApplicationName => "GenerationRules";
        protected override string ProjectFile => "GenerationRules.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => _forms.Select((loaded, index) => ($"v21-{index}", loaded.Model));

        protected override ArtifactRenderPlan Render(ExecutableSemanticModel model)
        {
            var loaded = _forms.First(form => form.Model.Revision == model.Revision);
            return CratisRendering.Plan(
                model,
                loaded.Plan,
                new(ArtifactRenderScopeKind.Application, model.Application.Id),
                new("GenerationRules", "GenerationRules"),
                loaded.ImplementationRequirements,
                loaded.ImplementationContents,
                loaded.AttachmentDiagnostics,
                loaded.TypedContextDescriptors);
        }

        Task Because()
        {
            AddGeneratedSpecification("GenerationBodyProbe.cs", """
                #if DEBUG
                namespace GenerationRules.Probes;
                public class GenerationBodyProbe
                {
                    [Xunit.Fact]
                    public void should_run_the_portable_body_with_the_supplied_context()
                    {
                        var instant = new global::System.DateTimeOffset(2024, 2, 29, 12, 0, 0, global::System.TimeSpan.Zero);
                        var context = new global::Cratis.Concepts.GenerationContext(instant, "tenant");
                        var value = global::GenerationRules.Common.TenantOrderId.New(context).Value;
                        Xunit.Assert.Matches("^tenant:order_20240229-[0-9a-f]{32}$", value);
                    }
                }
                #endif
                """);
            return Verify();
        }
    }
}
#endif
