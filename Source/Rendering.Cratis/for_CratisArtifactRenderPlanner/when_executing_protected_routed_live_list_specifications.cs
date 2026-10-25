// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_protected_routed_live_list_specifications : a_generated_application
{
    string _build = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_protected_collection_model.KeyedRouted));

    async Task Because()
    {
        _build = await Run("protected-routed-list-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        _tests = await Run("protected-routed-list-tests.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact] void should_build_without_warnings() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_pass_the_keyed_routed_row_expectation() => _tests.ShouldHavePassedSpecifications();
    [Fact] void should_pin_the_routed_instances() => ReadGeneratedFile("Billing/Invoicing/Lookup/when_listing_routed_invoices_is_queried.cs").ShouldContain("_scenario.Given.ForEventSource(instance.Key).ReadModel(instance.Value)");
}
#endif
