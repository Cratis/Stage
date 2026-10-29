// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_unobservable_event_source_specifications : a_generated_application
{
    protected override bool DeferPlan => true;
    [Fact]
    public async Task should_compile_and_execute_property_keyed_facts()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(when_checking_sourceless_event_consumers.Source));
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        await CompileAndExecute(plan);
    }

    [Fact]
    public async Task should_compile_and_execute_composite_keyed_recreation()
    {
        var model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        slice = slice with { Specifications = [slice.Specifications.Single(specification => specification.Name == "RecreateRemovedParts")], Queries = [] };
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] });
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        await CompileAndExecute(plan);
    }
    async Task CompileAndExecute(ArtifactRenderPlan plan)
    {
        try
        {
            RenderApplication(plan);
            var build = await Run("sourceless-build.log", "build", "InvoiceApp.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var tests = await Run("sourceless-test.log", "test", "InvoiceApp.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", tests, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed!", tests, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
