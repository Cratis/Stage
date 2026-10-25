// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_binding_pattern_and_random_common_types : Specification
{
    ArtifactRenderPlan _plan = null!;
    PlannedArtifact[] _restored = null!;
    RenderedFile[] _qualified = null!;
    RenderedFile[] _rendered = null!;
    QualifiedOutputResult _proof = null!;

    void Establish()
    {
        var model = invoice_model.Compile(a_pattern_random_model.Source
            .Replace("generate pattern \"{yyyy}-{MM}-{dd}-?{16}\"", "generate pattern \"{yyyy}-{MM}-{dd}-#{16}\" checksum luhn", StringComparison.Ordinal)
            .Replace("2028-02-29-ABCDEFGHIJKLMNOP", "2028-02-29-00000000000000000", StringComparison.Ordinal));
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join('\n', execution.Issues));
        var options = new CratisRenderingOptions("PatternRandom", "PatternRandom");
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), options);
        Assert.True(_plan.Success, string.Join('\n', _plan.Diagnostics));
        _restored = [.. QualifiedCommonOutput.Restore(model, _plan, options)];
        _qualified = Common(_restored);
        _rendered = Common(_plan.Artifacts);
    }

    void Because() => _proof = QualifiedOutputProof.VerifyRendered(_qualified, _rendered);

    [Fact] void should_compile_the_qualified_oracle() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_qualified));
    [Fact] void should_compile_the_real_short_output() => Assert.Empty(QualifiedOutputProof.CompilationErrors(_rendered));
    [Fact] void should_preserve_every_binding() => Assert.Empty(_proof.Failures);
    [Fact] void should_exercise_short_spellings() => _proof.ShortSpellings.ShouldBeGreaterThan(10);
    [Fact] void should_resolve_the_dated_checksum_factory() => File(_rendered, "DatedReference").ShouldContain("GenerateValue.Pattern(\"{yyyy}-{MM}-{dd}-#{16}\", context, Checksum.Luhn)");
    [Fact] void should_keep_the_qualified_dated_checksum_oracle() => File(_qualified, "DatedReference").ShouldContain("global::Cratis.Concepts.GenerateValue.Pattern(\"{yyyy}-{MM}-{dd}-#{16}\", context, global::Cratis.Concepts.Checksum.Luhn)");
    [Fact] void should_resolve_the_context_type() => File(_rendered, "DatedReference").ShouldContain("New(GenerationContext context)");
    [Fact] void should_resolve_the_contextual_interface() => File(_rendered, "DatedReference").ShouldContain("IGeneratableInContext<DatedReference>");
    [Fact] void should_preserve_the_int64_factory_bounds() => File(_rendered, "Ticket").ShouldContain("GenerateValue.Integer(-9223372036854775808L, 9223372036854775807L)");
    [Fact] void should_rely_on_the_global_using_for_the_factory() => File(_rendered, "DatedReference").StartsWith("namespace PatternRandom.Common;\n\n/// <summary>", StringComparison.Ordinal).ShouldBeTrue();

    // Since #424 every generated C# file is spelled short; the restore re-plans the qualified spelling of all of them.
    [Fact]
    public void should_confine_output_changes_to_csharp_spelling_and_using_placement()
    {
        var changed = _plan.Artifacts.Where(artifact => artifact.Sha256 != _restored.Single(original => original.RelativePath == artifact.RelativePath).Sha256)
            .Select(artifact => artifact.RelativePath).ToArray();
        Assert.All(changed, path => Assert.True(path == ".editorconfig" || path.EndsWith(".cs", StringComparison.Ordinal), path));
        Assert.Contains(".editorconfig", changed);
    }

    // Custom types live in their own namespaces; the global namespace, the root namespace and global aliases bind
    // before any using directive, so they are outside the contract (see Customize a rendered application).
    [Theory]
    [InlineData("namespace Customizations { public class Checksum; public class GenerationContext; }")]
    [InlineData("namespace Customizations.Cratis.Concepts { public class GenerateValue; public enum Checksum { Luhn } }")]
    [InlineData("namespace PatternRandom.Cratis.Concepts { public class GenerateValue; public enum Checksum { Luhn } }")]
    public void should_preserve_bindings_with_hostile_outer_names(string source)
    {
        var hostile = new RenderedFile("Customizations/Hostile.cs", source);
        var qualified = _qualified.Append(hostile).ToArray();
        var rendered = _rendered.Append(hostile).ToArray();
        Assert.Empty(QualifiedOutputProof.CompilationErrors(qualified));
        Assert.Empty(QualifiedOutputProof.CompilationErrors(rendered));
        Assert.Empty(QualifiedOutputProof.VerifyRendered(qualified, rendered).Failures);
    }

    static RenderedFile[] Common(IEnumerable<PlannedArtifact> artifacts) => [.. artifacts
        .Where(artifact => artifact.RelativePath.StartsWith("Common/", StringComparison.Ordinal))
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];

    static string File(IEnumerable<RenderedFile> files, string name) => files.Single(file => file.RelativePath == $"Common/{name}.cs").Content;
}
#endif
