// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommonArtifactRenderer;

public class when_rendering_unused_generation_strategies : Specification
{
    SemanticApplicationContext _context = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _context = declared_generation.Context();
    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_context.Request);

    [Theory]
    [InlineData("StringUuid", "GenerateValue.Uuid()", false)]
    [InlineData("Sortable", "GenerateValue.UuidV7(context).ToString(\"D\").ToUpperInvariant()", true)]
    [InlineData("UlidRef", "GenerateValue.Ulid(context).ToLowerInvariant()", true)]
    [InlineData("PersonId", "GenerateValue.TypeId(\"person\", context)", true)]
    [InlineData("ShortCode", "GenerateValue.NanoId(8, \"ABC123\")", false)]
    [InlineData("HexReference", "RandomNumberGenerator.GetHexString(32, false)", false)]
    [InlineData("Base32Reference", "GenerateValue.Bytes(16, ValueEncoding.Base32).ToLowerInvariant()", false)]
    [InlineData("UrlReference", "GenerateValue.Bytes(16, ValueEncoding.Base64Url)", false)]
    [InlineData("CheckedReference", "GenerateValue.Pattern(\"#{16}\", Checksum.Luhn)", false)]
    [InlineData("DatedReference", "GenerateValue.Pattern(\"{yyyy}-{MM}-{dd}-?{16}\", context)", true)]
    [InlineData("TicketNumber", "GenerateValue.Integer(-9223372036854775808L, 9223372036854775807L)", false)]
    void should_emit_the_declared_strategy_even_without_a_generation_use(string name, string factory, bool contextual)
    {
        var source = Source(name);
        Assert.Contains(contextual ? $"IGeneratableInContext<{name}>" : $"IGeneratable<{name}>", source);
        Assert.Contains(contextual ? "New(GenerationContext context)" : "New()", source);
        Assert.Contains(factory, source);
    }

    [Fact]
    void should_preserve_the_generation_rule_and_body()
    {
        var source = Source("TenantOrderId");
        var concept = _context.Application.Concepts.Single(concept => concept.Name == "TenantOrderId");
        Assert.Contains("New(GenerationContext context)", source);
        Assert.DoesNotContain(SemanticGenerationRendering.BodyMethod(concept, _context) + "(context)", source);
        Assert.Contains("return \"Generated\";", source);
    }

    [Fact] void should_preserve_the_uuid_prefix() => Assert.Contains("\"evt-\" + GenerateValue.UuidV7(context)", Source("Sortable"));
    [Fact] void should_use_the_collision_safe_factory_name() => Assert.Contains("static New IGeneratable<New>.New()", Source("New"));
    [Fact] void should_not_give_ordinary_text_a_factory() => Assert.DoesNotContain("static OrdinaryText New(", Source("OrdinaryText"));
    [Fact] void should_not_give_an_unused_ordinary_uuid_a_factory() => Assert.DoesNotContain("static OrdinaryUuid New(", Source("OrdinaryUuid"));

    [Fact]
    void should_compile_the_rendered_application()
    {
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal))
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        RenderedOutput.Errors(files).ShouldBeEmpty();
    }

    string Source(string name)
    {
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        var artifact = _plan.Artifacts.Single(artifact => artifact.RelativePath == $"Common/{name}.cs");
        return Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
    }
}
