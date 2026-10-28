// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_generated_source_finalization
{
    [Fact]
    void should_convert_the_actual_import_finalizer_throw_into_a_refused_plan_without_artifacts()
    {
        const string expected = "A member named 'nameof' prevents proving the generated nameof binding.";
        const string controlBody = "var rows = readModels; return rows;";
        const string refusalBody = "var nameof = readModels; return nameof;";
        var control = FinalizerRequest(controlBody);
        var (controlContext, controlSlice) = AdmittedFinalizerContext(control, controlBody);
        var controlFile = SemanticStateViewArtifactRenderer.Render(controlSlice, controlContext);
        Assert.Contains(controlBody, controlFile.Content);
        var controlPlan = new CratisArtifactRenderPlanner().Plan(control);
        Assert.True(controlPlan.CanPublishArtifacts, string.Join("; ", controlPlan.Diagnostics));
        Assert.NotEmpty(controlPlan.Artifacts);

        var request = FinalizerRequest(refusalBody);
        var (context, slice) = AdmittedFinalizerContext(request, refusalBody);
        var failure = Assert.Throws<CSharpImportRejected>(() => SemanticStateViewArtifactRenderer.Render(slice, context));
        Assert.Equal(expected, failure.Message);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        var diagnostic = Assert.Single(plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Equal("STAGE-CSHARP-IMPORT", diagnostic.Code);
        Assert.Equal(expected, diagnostic.Message);
        Assert.Equal(request.Model.Application.Id, diagnostic.Artifact);
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        Assert.False(plan.Success);
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
        Assert.Equal(refusalBody, Assert.Single(request.ImplementationContents).Value);
        Assert.Equal(controlBody, Assert.Single(control.ImplementationContents).Value);
    }

    [Theory]
    [InlineData("int unused = 42; ")]
    [InlineData("System.Exception unused; ")]
    void should_refuse_an_unused_command_local_under_the_scaffold_warning_policy(string prefix)
    {
        var plan = handler_model.Plan(handler_model.Request(prefix + handler_model.Body));
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains("unused", StringComparison.Ordinal));
    }

    [Fact]
    void should_respect_suppressed_warnings_and_ignore_hidden_diagnostics_in_the_compiler_policy()
    {
        var tree = CSharpSyntaxTree.ParseText(SemanticBodyCompilation.ImplicitUsings + "using System.Text;\n#pragma warning disable CS0219\ninternal class Probe { public void Run() { int unused = 42; } }");
        var compilation = SemanticBodyCompilation.Create("SuppressionProbe", tree);
        Assert.Empty(compilation.GetDiagnostics().Where(SemanticBodyCompilation.IsFailure));
        Assert.Contains(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Hidden && !SemanticBodyCompilation.IsFailure(diagnostic));
        var unsuppressed = CSharpSyntaxTree.ParseText(tree.ToString().Replace("#pragma warning disable CS0219", string.Empty, StringComparison.Ordinal));
        Assert.Contains(SemanticBodyCompilation.Create("UnsuppressedProbe", unsuppressed).GetDiagnostics(), diagnostic => diagnostic.Id == "CS0219" && SemanticBodyCompilation.IsFailure(diagnostic));
    }

    [Fact]
    void should_accept_a_used_command_local_without_rewriting_its_body()
    {
        const string body = "var name = context.Command.Name; return new OrderPlaced(name);";
        var plan = handler_model.Plan(handler_model.Request(body));
        Assert.True(plan.CanPublishArtifacts, string.Join("; ", plan.Diagnostics));
        Assert.Contains(plan.Artifacts, artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains(body, StringComparison.Ordinal));
    }

    static ArtifactRenderRequest FinalizerRequest(string body)
    {
        var source = when_rendering_authoring_documentation.Source;
        var modelStart = source.IndexOf("      readmodel ProjectSummary", StringComparison.Ordinal);
        var modelEnd = source.IndexOf("      query ProjectById", StringComparison.Ordinal);
        Assert.True(modelStart >= 0 && modelEnd > modelStart);
        var model = source[modelStart..modelEnd];
        Assert.Equal(2, model.Split("        name ProjectName", StringSplitOptions.None).Length);
        source = source[..modelStart] + model.Replace("        name ProjectName", "        projectName ProjectName", StringComparison.Ordinal) + source[modelEnd..];
        var projectionStart = source.IndexOf("      projection ProjectSummaryProjection", StringComparison.Ordinal);
        Assert.True(projectionStart >= 0);
        var projection = source[projectionStart..];
        Assert.Equal(2, projection.Split("          name = name", StringSplitOptions.None).Length);
        source = source[..projectionStart] + projection.Replace("          name = name", "          projectName = name", StringComparison.Ordinal);
        source += $"\n      query AllProjects => ProjectSummary[]\n        performer\n          ```csharp\n          {body}\n          ```\n";
        var loaded = opaque_policy_model.Load(source);
        return new(loaded.Model, loaded.Plan, CratisRendering.CreateProfile(loaded.Model.Application.Name, new("Projects", "Projects")), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            IdentityCatalog = loaded.IdentityCatalog,
            SemanticIdentities = loaded.SemanticIdentities,
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
    }

    static (SemanticApplicationContext Context, LocatedSemanticSlice Slice) AdmittedFinalizerContext(ArtifactRenderRequest request, string body)
    {
        Assert.Empty(SemanticImplementationAdmission.Verify(request));
        Assert.True(CratisArtifactRenderProfileAdmission.Matches(request, out var options, out var diagnostics), string.Join("; ", diagnostics));
        var context = new SemanticApplicationContext(request, options);
        context.ScopeFlatProjections();
        var slice = Assert.Single(context.SelectedSlices(), slice => slice.Slice.Name == "ProjectLookup");
        var projection = Assert.Single(slice.Slice.Projections);
        Assert.Null(projection.Scope);
        var transition = Assert.Single(projection.Transitions);
        var model = Assert.Single(slice.Slice.ReadModels);
        var query = Assert.Single(slice.Slice.Queries, query => query.Name == "AllProjects");
        Assert.Single(request.ImplementationRequirements, requirement => requirement.Role == SemanticImplementationRole.QueryPerformer);
        var admission = SemanticImplementationAdmission.AdmitPerformer(context, query);
        Assert.Null(admission.Reason);
        Assert.Equal(body, admission.Body);
        var key = Assert.Single(slice.Slice.Queries, query => query.Name == "ProjectById");
        var parameters = SemanticStateViewArtifactRenderer.Parameters(model, transition, context.Events[transition.EventContract], new SemanticTypeSystem(context), key);
        Assert.Contains("nameof(global::", parameters);
        return (context, slice);
    }
}
