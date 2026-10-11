// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_values_and_responses : a_generated_application
{
    string _build = null!;
    string _test = null!;

    protected override ArtifactRenderPlan CreatePlan() => invoice_model.Plan(invoice_model.Compile(a_generated_values_model.Source));

    async Task Because()
    {
        _build = await Run("generated-values-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
        _test = await Run("generated-values-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
    }

    [Fact] void should_compile_without_warnings() => BuildWarnings(_build).ShouldEqual(string.Empty);
    [Fact] void should_execute_pinned_scalar_record_and_unpinned_relational_specifications() => _test.ShouldContain("Passed!");
}
#endif
