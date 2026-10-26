// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode.when_verifying_original_bytes;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticInlineCode;

public class when_rendering_verified_generation_body : Specification
{
    const string Source = """
        concept OrderId : String
          generate rule TenantOrder
            ```csharp
            return "Generated";
            ```
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

    [Fact]
    void should_use_verified_envelope_and_leave_semantic_body_out_of_finish()
    {
        var context = Context("return \"Generated\";");
        var builder = new CSharpCodeBuilder().OpenBlock("class Generated").ExpressionMember("string GeneratedValue()", "\"Generated\"");
        SemanticGenerationRendering.RenderBody(builder, context.Application.Concepts.Single(), context);
        builder.EndBlock();
        var output = builder.ToString(skeleton =>
        {
            Assert.DoesNotContain("return \"Generated\";", skeleton);
            return skeleton.Replace("\"Generated\"", "\"Finished\"", StringComparison.Ordinal);
        });
        Assert.Contains("class Generated", output);
        Assert.Contains("string GeneratedValue() => \"Finished\";", output);
        Assert.Contains("return \"Generated\";", output);
        Assert.DoesNotContain(InlineCodeTokens.Prefix, output);
    }

    [Theory]
    [InlineData("return \"Generated\";")]
    [InlineData("return @\"unterminated")]
    void should_refuse_mismatched_hash_at_actual_semantic_entrypoint(string body)
    {
        var context = Context(body, "different verified bytes");
        var builder = new CSharpCodeBuilder();
        var outcome = SemanticInlineCodeRendering.Generation(context.Application.Concepts.Single(), context);
        outcome.Refused.ShouldBeTrue();
        outcome.Degraded.ShouldBeFalse();
        Assert.All(outcome.Diagnostics, diagnostic => Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity));
        var exception = Assert.Throws<InvalidArtifactRenderContract>(() => SemanticGenerationRendering.RenderBody(builder, context.Application.Concepts.Single(), context));
        Assert.Contains("SHA-256 differs", exception.Message);
        Assert.DoesNotContain(body, builder.ToString());
    }

    [Fact]
    void should_label_string_contract_honestly_without_claiming_original_disk_bytes()
    {
        var context = Context("return \"Generated\";");
        var requirement = context.Request.ImplementationRequirements.Single();
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var owner = context.Application.Concepts.Single().Id;
        var site = new SemanticInlineCodeSite(requirement.RequirementId, owner, new("generation", owner.ToString(), "member"));
        SemanticImplementationAdmission.TryGetVerifiedInline(context.Request, site, diagnostics, out var code).ShouldBeTrue();
        code!.ByteSource.ShouldEqual("utf8-string");
        Assert.Empty(diagnostics);
    }

    [Fact]
    void should_retain_nonempty_generated_output_and_exact_warning_for_provably_safe_foreign_mismatch()
    {
        var context = Context(" \n", language: "java");
        var outcome = SemanticInlineCodeRendering.Generation(context.Application.Concepts.Single(), context);
        outcome.Refused.ShouldBeFalse();
        outcome.Degraded.ShouldBeTrue();
        var warning = Assert.Single(outcome.Diagnostics);
        warning.Code.ShouldEqual("STAGE-DEGRADE-003");
        warning.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Warning);
        warning.Message.ShouldEqual("The body language does not match csharp.");
        var builder = context.CodeBuilder().OpenBlock("class Host");
        SemanticGenerationRendering.RenderBody(builder, context.Application.Concepts.Single(), context);
        var source = builder.EndBlock().ToString();
        Assert.Contains("class Host", source);
        Assert.Contains("throw new NotSupportedException", source);
        Assert.DoesNotContain("global::System.NotSupportedException", source);
        Assert.DoesNotContain("return \"Generated\";", source);
        Assert.NotNull(outcome.EffectiveInputFingerprint);
        Assert.Equal(warning, Assert.Single(context.InlineDiagnostics));
    }

    static SemanticApplicationContext Context(string body, string? hashSource = null, string language = "csharp")
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("shop"), "shop", "Shop.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var options = new CratisRenderingOptions("Shop", "Shop");
        var requirements = compilation.ImplementationRequirements.Select(requirement => requirement with { ContentHash = Hash(Encoding.UTF8.GetBytes(hashSource ?? body)), Language = language }).ToImmutableArray();
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Shop", options), new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = requirements,
            ImplementationContents = requirements.ToImmutableDictionary(requirement => requirement.RequirementId, _ => body)
        };
        return new(request, options);
    }
}
