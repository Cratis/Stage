// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_validated_query_keys(when_executing_validated_query_keys.context fixture) : IClassFixture<when_executing_validated_query_keys.context>
{
    [Fact] void should_build_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_accept_valid_and_out_of_rule_lookup_and_filter_keys_without_validation_results() => fixture.Results.ShouldContain("Passed!");

    public class context : a_generated_application
    {
        public string DebugWarnings { get; private set; } = string.Empty;
        public string Results { get; private set; } = string.Empty;

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_validated_query_key_model.Source));

        async Task Because()
        {
            // SDK source generators assign event identities and register projections; a bare
            // Roslyn load cannot exercise Chronicle's discovery through the real query pipeline.
            AddGeneratedSpecification("QueryKeys.cs", a_validated_query_key_model.Probe + "\n" + """
                public class validated_keys
                {
                    [Fact]
                    public Task should_preserve_lookup_and_filtered_answers() => QueryKeys.Execute();
                }
                """);
            DebugWarnings = BuildWarnings(await Run("validated-query-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            Results = await Run("validated-query-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
        }
    }
}
#endif
