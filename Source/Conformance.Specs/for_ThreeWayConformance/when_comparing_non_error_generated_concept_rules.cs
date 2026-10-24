// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_non_error_generated_concept_rules(when_comparing_non_error_generated_concept_rules.context fixture) : IClassFixture<when_comparing_non_error_generated_concept_rules.context>
{
    [Fact] void should_preserve_both_severities_on_all_three_paths() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_have_no_three_way_differences() => Assert.Empty(fixture.Differences);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "NonErrorGeneration";
        protected override string ProjectFile => "NonErrorGeneration.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("non-error-rules", invoice_model.Compile(Source))];

        Task Because()
        {
            AddGeneratedSpecification("GenerationDefectProbe.cs", """
                #if DEBUG
                using Cratis.Arc.Testing.Commands;
                using Xunit;
                namespace NonErrorGeneration.Probes;
                public class GenerationDefectProbe
                {
                    [Fact]
                    public async System.Threading.Tasks.Task should_reject_a_generated_value_failing_a_warning_rule()
                    {
                        using var scenario = new CommandScenario<global::NonErrorGeneration.M.F.Warning.CreateWarning>();
                        scenario.PinGeneratedValue("code", new global::NonErrorGeneration.Common.WarningCode("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab"));
                        var result = await scenario.Execute(new global::NonErrorGeneration.M.F.Warning.CreateWarning(), Xunit.TestContext.Current.CancellationToken);
                        Assert.True(result.HasExceptions);
                        Assert.Empty(result.ValidationResults);
                        Assert.False(result.IsSuccess);
                    }
                    [Fact]
                    public async System.Threading.Tasks.Task should_reject_a_generated_value_failing_an_information_rule()
                    {
                        using var scenario = new CommandScenario<global::NonErrorGeneration.M.F.Information.CreateInformation>();
                        scenario.PinGeneratedValue("code", new global::NonErrorGeneration.Common.InformationCode("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab"));
                        var result = await scenario.Execute(new global::NonErrorGeneration.M.F.Information.CreateInformation(), Xunit.TestContext.Current.CancellationToken);
                        Assert.True(result.HasExceptions);
                        Assert.Empty(result.ValidationResults);
                        Assert.False(result.IsSuccess);
                    }
                }
                #endif
                """);
            return Verify();
        }

        static readonly string Source = $$"""
            concept WarningCode : String
              generate nanoid 64 alphabet "ab"
              validate
                matches "{{invoice_model.UnprovenCodePattern}}" severity warning
            concept InformationCode : String
              generate nanoid 64 alphabet "ab"
              validate
                matches "{{invoice_model.UnprovenCodePattern}}" severity information
            module M
              feature F
                slice StateChange Warning
                  command CreateWarning
                    code WarningCode generated
                    returns code
                  specification GeneratingWithAWarningRule
                    when CreateWarning
                      generated code = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                    then returns "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                slice StateChange Information
                  command CreateInformation
                    code InformationCode generated
                    returns code
                  specification GeneratingWithAnInformationRule
                    when CreateInformation
                      generated code = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                    then returns "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            """;
    }
}
#endif
