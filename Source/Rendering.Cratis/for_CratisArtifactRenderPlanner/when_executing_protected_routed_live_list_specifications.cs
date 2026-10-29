// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_protected_routed_live_list_specifications(when_executing_protected_routed_live_list_specifications.context fixture) : IClassFixture<when_executing_protected_routed_live_list_specifications.context>
{
    [Fact] void should_build_without_warnings() => fixture.BuildWarningLines.ShouldEqual(string.Empty);
    [Fact] void should_pass_the_keyed_routed_row_expectation() => fixture.Tests.ShouldContain("Passed!");
    [Fact] void should_pin_the_routed_instances() => fixture.GeneratedFile("Billing/Invoicing/Lookup/when_listing_routed_invoices_is_queried.cs").ShouldContain("_scenario.Given.ForEventSource(instance.Key).ReadModel(instance.Value)");

    public class context : a_generated_application
    {
        protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_protected_collection_model.KeyedRouted));

        async Task Because()
        {
            Build = await Run("protected-routed-list-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Tests = await Run("protected-routed-list-tests.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
        }

        public string Build { get; private set; } = null!;
        public string Tests { get; private set; } = null!;
        public string BuildWarningLines => BuildWarnings(Build);
        public string GeneratedFile(string relativePath) => ReadGeneratedFile(relativePath);
    }
}
#endif
