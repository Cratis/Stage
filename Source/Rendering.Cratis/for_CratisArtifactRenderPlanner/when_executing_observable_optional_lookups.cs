// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_observable_optional_lookups(when_executing_observable_optional_lookups.context fixture) : IClassFixture<when_executing_observable_optional_lookups.context>
{
    [Fact] void should_build_without_warnings() => fixture.Warnings.ShouldEqual(string.Empty);
    [Fact] void should_execute_the_companion_and_runtime_specifications() => fixture.Results.ShouldContain("Passed!");
    [Fact] void should_use_the_first_live_result_in_the_companion() => fixture.Companion.ShouldContain("Observable.FirstAsync");
    [Fact] void should_use_real_chronicle_observation_in_the_companion() => fixture.Companion.ShouldNotContain("Substitute");

    public class context : a_generated_application
    {
        public string Warnings { get; private set; } = string.Empty;
        public string Results { get; private set; } = string.Empty;
        public string Companion { get; private set; } = string.Empty;

        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(an_observable_lookup_model.Source));

        async Task Because()
        {
            Companion = ReadGeneratedFile("Billing/Invoicing/Lookup/when_looking_up_issued_invoice_is_queried.cs");
            AddGeneratedSpecification("ObservableLookups.cs", an_observable_lookup_probe.Source);
            Warnings = BuildWarnings(await Run("observable-lookup-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo"));
            Results = await Run("observable-lookup-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
        }
    }
}
#endif
