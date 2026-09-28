// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Names the renderer declares outside modeled namespaces (policy registration, reducer runtime)
/// and read models built by reducers get the same admission as everything else: a plan either
/// fails with STAGE-ESM-012, or every emitted C# file compiles.
/// </summary>
public class when_planning_names_beside_generated_declarations
{
    [Fact]
    public void should_compile_policies_when_a_module_is_named_like_a_framework_type()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(
            when_rendering_portable_authorization.Source.Replace("module Billing", "module StringComparison", StringComparison.Ordinal)));

        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Assert.Empty(RenderedOutput.Errors(CSharp(plan)));
    }

    [Fact]
    public void should_reject_a_feature_that_declares_the_policy_registration_namespace()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(when_rendering_portable_authorization.Source
            .Replace("module Billing", "module GeneratedPolicies", StringComparison.Ordinal)
            .Replace("feature Invoicing", "feature Registration", StringComparison.Ordinal)));

        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("Registration", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("query Total => Total?\n        by id Uuid")]
    [InlineData("query Amount => Total?\n        by id Uuid")]
    public async Task should_reject_a_reducer_backed_query_named_like_a_member(string query)
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(ReducerSource()
            .Replace("query ById => Total?\n        by id Uuid", query, StringComparison.Ordinal)));

        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("Total", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_reject_a_reducer_backed_query_argument_named_like_the_read_models_parameter()
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(ReducerSource()
            .Replace("      readmodel Total\n        id Uuid", "      readmodel Total\n        readModels Uuid", StringComparison.Ordinal)
            .Replace("by id Uuid", "by readModels Uuid", StringComparison.Ordinal)));

        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("Total", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_admit_and_compile_an_ordinary_reducer_backed_query()
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(ReducerSource()));

        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Assert.Empty(RenderedOutput.Errors(CSharp(plan)));
    }

    static string ReducerSource() => when_rendering_a_pure_reducer.Source
        .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);

    static IEnumerable<RenderedFile> CSharp(ArtifactRenderPlan plan) => plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))
        .ToArray();
}
