// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticInlineCode.when_rendering_verified_generation_body;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_retaining_inline_input_provenance : Specification
{
    [Fact]
    void should_report_the_authenticated_fingerprint_and_repeat_it_deterministically()
    {
        var request = Request("return \"Generated\";");
        var first = Plan(request);
        var second = Plan(request);
        Fingerprint(first).ShouldEqual(Expected(request));
        Fingerprint(second).ShouldEqual(Fingerprint(first));
        second.Digest.ShouldEqual(first.Digest);
    }

    [Theory]
    [InlineData("return \"Generated\";\n")]
    [InlineData("\uFEFFreturn \"Generated\";")]
    void should_change_plan_provenance_when_original_bytes_change_but_output_does_not(string body)
    {
        var original = Plan(Request("return \"Generated\";"));
        var changedRequest = Request(body);
        var changed = Plan(changedRequest);
        Fingerprint(changed).ShouldEqual(Expected(changedRequest));
        Fingerprint(changed).ShouldNotEqual(Fingerprint(original));
        changed.Digest.ShouldNotEqual(original.Digest);
        Assert.Equal(Generated(original), Generated(changed));
    }

    [Theory]
    [InlineData("java", false)]
    [InlineData("java", true)]
    void should_retain_language_and_placement_even_for_safe_degradation(string language, bool rename)
    {
        var original = DirectPlan(Request(" \n", rename ? language : "csharp"));
        var request = Request(" \n", language, rename);
        var changed = DirectPlan(request);
        Fingerprint(changed).ShouldEqual(Expected(request));
        Fingerprint(changed).ShouldNotEqual(Fingerprint(original));
        changed.Digest.ShouldNotEqual(original.Digest);
        Assert.Contains(changed.Diagnostics, diagnostic => diagnostic.Code == "STAGE-DEGRADE-003");
    }

    [Fact]
    void should_keep_provenance_through_reports_layout_acceptance_and_diagnostic_reconstruction()
    {
        var request = Request("return \"Generated\";") with
        {
            Profile = CratisRendering.CreateProfile("Shop", new("Shop", "Shop") { ServiceLayout = true })
        };
        var plan = Plan(request);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "Source/Shop/.stage/completeness.json");
        var strict = plan.WithAcceptance(plan.Degradations, strict: true);
        var rebuilt = CratisPlanDiagnosticReconstruction.Append(request, strict, []);
        var report = ArtifactRenderReports.Create(rebuilt, ReportContext(rebuilt)).Completeness;
        using var json = JsonDocument.Parse(report.Bytes.ToArray());
        json.RootElement.GetProperty("inlineCodeFingerprint").GetString().ShouldEqual(Expected(request));
        Fingerprint(plan).ShouldEqual(Expected(request));
    }

    [Fact]
    void should_include_every_rendered_body_independently_of_requirement_order()
    {
        var request = Request("return \"Generated\";", multiple: true);
        var plan = Plan(request);
        var reordered = Plan(request with { ImplementationRequirements = [.. request.ImplementationRequirements.Reverse()] });
        Fingerprint(plan).ShouldEqual(Expected(request));
        Fingerprint(plan).ShouldNotEqual(Fingerprint(Plan(Request("return \"Generated\";"))));
        Fingerprint(reordered).ShouldEqual(Fingerprint(plan));
        reordered.Digest.ShouldEqual(plan.Digest);
    }

    [Fact]
    void should_not_hash_requirements_that_were_not_rendered()
    {
        var request = Request("return \"Generated\";");
        var plan = DirectPlan(request);
        var unused = request.ImplementationRequirements.Single() with { RequirementId = "unused" };
        var changed = DirectPlan(request with { ImplementationRequirements = [unused, .. request.ImplementationRequirements] });
        Fingerprint(changed).ShouldEqual(Fingerprint(plan));
        changed.Digest.ShouldEqual(plan.Digest);
    }

    [Fact]
    void should_leave_plans_without_inline_code_without_provenance()
    {
        var request = Request("return \"Generated\";");
        var plan = ArtifactRenderPlan.Create(request, [], []).WithInventory(ArtifactRenderInventory.Create([]));
        var report = ArtifactRenderReports.Create(plan, ReportContext(plan)).Completeness;
        using var json = JsonDocument.Parse(report.Bytes.ToArray());
        Assert.False(json.RootElement.TryGetProperty("inlineCodeFingerprint", out _));
    }

    static ArtifactRenderPlan Plan(ArtifactRenderRequest request)
    {
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.CanPublishArtifacts, string.Join(';', plan.Diagnostics));
        return plan;
    }

    static ArtifactRenderPlan DirectPlan(ArtifactRenderRequest request)
    {
        var context = new SemanticApplicationContext(request, new("Shop", "Shop"));
        var concept = context.Application.Concepts.Single();
        var builder = new CSharpCodeBuilder().OpenBlock("class Generated");
        SemanticGenerationRendering.RenderBody(builder, concept, context);
        var body = builder.EndBlock().ToString();
        var plan = ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("OrderId.cs", body)], [.. context.InlineDiagnostics]);
        return CratisPlanReports.Finalize(request, context.WithInlineInputs(plan), new("Shop", "Shop"));
    }

    static string Fingerprint(ArtifactRenderPlan plan)
    {
        var report = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(".stage/completeness.json", StringComparison.Ordinal));
        using var json = JsonDocument.Parse(report.Bytes.ToArray());
        return json.RootElement.GetProperty("inlineCodeFingerprint").GetString()!;
    }

    static ArtifactRenderReportContext ReportContext(ArtifactRenderPlan plan) => new(plan.InputVersions!, new([plan.InputVersions!]), []);

    static string[] Generated(ArtifactRenderPlan plan) => [.. plan.Artifacts.Where(artifact => !artifact.RelativePath.EndsWith(".stage/completeness.json", StringComparison.Ordinal))
        .Select(artifact => artifact.RelativePath + ":" + artifact.Sha256)];

    static string Expected(ArtifactRenderRequest request)
    {
        var codes = request.ImplementationRequirements.Select(requirement =>
        {
            var concept = request.Model.Application.Concepts.Single(item => item.Generation!.RequirementId == requirement.RequirementId);
            var placement = new InlineCodePlacement("generation", concept.Id.ToString(), SemanticGenerationRendering.BodyMethod(concept));
            return InlineCode.VerifyUtf8Text(request.ImplementationContents[requirement.RequirementId],
                new(requirement.ContentHash!, requirement.Language!, placement, requirement.RequirementId)).Value!;
        });
        return CSharpInlineLanguagePack.Fingerprint(codes);
    }

    static ArtifactRenderRequest Request(string body, string language = "csharp", bool rename = false, bool multiple = false)
    {
        var seed = Context(body, language: language).Request;
        var source = """
            concept OrderId : String
              generate rule TenantOrder
                ```csharp
                return "Generated";
                ```
            """;
        if (multiple) source += "\n" + source.Replace("OrderId", "InvoiceId", StringComparison.Ordinal);
        source += "\n" + """
            module Shop
              feature Orders
                slice StateChange Create
                  command Create
                    streamId String identifier
                    id OrderId generated
                    produces Created
                      for streamId
                      id = id
                  event Created
                    id OrderId
            """;
        if (multiple) source = source.Replace("id OrderId generated", "id OrderId generated\n        other InvoiceId generated", StringComparison.Ordinal);
        if (rename) source = source.Replace("OrderId", "InvoiceId", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", source);
        var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var compiled = compilation.Value!;
        var requirements = compilation.ImplementationRequirements.Select(requirement => requirement with
        {
            ContentHash = seed.ImplementationRequirements.Single().ContentHash,
            Language = language
        }).ToImmutableArray();
        return new(
            compiled.Model,
            SemanticExecutionPlan.Compile(compiled.Model).Plan!,
            CratisRendering.CreateProfile("Shop", new("Shop", "Shop")),
            new(ArtifactRenderScopeKind.Application, compiled.Model.Application.Id))
        {
            IdentityCatalog = compiled.Documents.IdentityCatalog,
            SemanticIdentities = compiled.Documents.IdentityCatalog.Semantics,
            ImplementationRequirements = requirements,
            TypedContextDescriptors = compilation.TypedContextDescriptors,
            ImplementationContents = requirements.ToImmutableDictionary(requirement => requirement.RequirementId, _ => body)
        };
    }
}
