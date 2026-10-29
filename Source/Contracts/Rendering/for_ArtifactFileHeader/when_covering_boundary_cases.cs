// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactFileHeader;

public class when_covering_boundary_cases : Specification
{
    static ArtifactRenderProfile Profile(params ArtifactRenderInput[] inputs) => ArtifactRenderProfile.Create("test", "1", "test", "1", [.. inputs]);

    [Fact] void should_treat_default_lines_as_no_header()
    {
        ArtifactFileHeader.IsValid(default).ShouldBeTrue();
        var profile = Profile();
        ArtifactFileHeader.With(profile, default).ShouldEqual(profile);
        ArtifactFileHeader.TryRead(profile, out var lines).ShouldBeTrue();
        lines.ShouldBeEmpty();
    }

    [Fact] void should_reject_empty_input_creation() => Catch.Exception(() => ArtifactFileHeader.CreateInput([])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_keep_the_canonical_identity()
    {
        var input = ArtifactFileHeader.CreateInput(["Ærlig ©", ""]);
        input.Name.ShouldEqual("stage:file-header");
        input.Version.ShouldEqual("1");
        ArtifactFileHeader.IsInput(input).ShouldBeTrue();
        ArtifactFileHeader.IsInput(ArtifactRenderInput.Create("stage:unrelated", "1", [])).ShouldBeFalse();
    }

    [Theory]
    [InlineData("\u061C")][InlineData("\u200E")][InlineData("\u200F")]
    [InlineData("\u202A")][InlineData("\u202B")][InlineData("\u202C")][InlineData("\u202D")][InlineData("\u202E")]
    [InlineData("\u2066")][InlineData("\u2067")][InlineData("\u2068")][InlineData("\u2069")]
    [InlineData("\u0000")][InlineData("\u007F")][InlineData("\u0085")][InlineData("\uE000")][InlineData("\U00100000")]
    [InlineData("\r")][InlineData("\n")][InlineData("\u2028")][InlineData("\u2029")]
    void should_refuse_unsafe_text_at_creation_and_reading(string line)
    {
        Catch.Exception(() => ArtifactFileHeader.CreateInput([line])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
        var input = ArtifactRenderInput.Create(ArtifactFileHeader.InputName, "1", [.. JsonSerializer.SerializeToUtf8Bytes(new[] { line })]);
        var result = ArtifactFileHeader.Apply(Profile(input), [], ArtifactCommentSyntaxes.Default);
        result.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
        result.Artifacts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("cratis-render:file-header")]
    [InlineData("axon-render:file-header")]
    void should_not_add_a_header_beside_an_alias(string name) => Catch.Exception(() => ArtifactFileHeader.With(Profile(ArtifactRenderInput.Create(name, "1", ArtifactFileHeader.CreateInput(["hello"]).Bytes)), ["hello"])).ShouldBeOfExactType<InvalidArtifactRenderContract>();

    [Fact] void should_refuse_malformed_utf8()
    {
        var input = ArtifactRenderInput.Create(ArtifactFileHeader.InputName, "1", [0xFF]);
        ArtifactFileHeader.Apply(Profile(input), [], ArtifactCommentSyntaxes.Default).Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
    }

    [Theory]
    [InlineData(".cs")][InlineData(".ts")][InlineData(".tsx")][InlineData(".mts")][InlineData(".cts")]
    [InlineData(".js")][InlineData(".jsx")][InlineData(".mjs")][InlineData(".cjs")]
    [InlineData(".kt")][InlineData(".kts")][InlineData(".go")][InlineData(".java")]
    void should_prepend_for_every_code_extension(string extension)
    {
        var profile = ArtifactFileHeader.With(Profile(), ["hello"]);
        var result = ArtifactFileHeader.Apply(profile, [PlannedArtifact.CreateText("Code" + extension, "code")], ArtifactCommentSyntaxes.Default);
        Encoding.UTF8.GetString(result.Artifacts.Single().Bytes.AsSpan()).ShouldEqual("// hello\n\ncode");
    }

    [Fact] void should_prepend_before_the_bom()
    {
        var profile = ArtifactFileHeader.With(Profile(), ["hello"]);
        var artifact = PlannedArtifact.CreateText("Code.cs", "\uFEFFcode");
        var result = ArtifactFileHeader.Apply(profile, [artifact], ArtifactCommentSyntaxes.Default);
        Encoding.UTF8.GetString(result.Artifacts.Single().Bytes.AsSpan()).ShouldEqual("// hello\n\n\uFEFFcode");
    }

    [Fact] void should_not_match_a_dot_in_a_parent_directory() => ArtifactCommentSyntaxes.Default.TryGet("directory.cs/code", out _).ShouldBeFalse();

    [Fact] void should_report_one_diagnostic_per_extension_in_stable_order()
    {
        var profile = ArtifactFileHeader.With(Profile(), ["blocked"]);
        var syntaxes = ArtifactCommentSyntaxes.Create(new Dictionary<string, ArtifactCommentSyntax>
        {
            [".z"] = new("//", ["blocked"]),
            [".a"] = new("//", ["blocked"])
        });
        ImmutableArray<PlannedArtifact> artifacts = [PlannedArtifact.CreateText("second.z", ""), PlannedArtifact.CreateText("last.a", ""), PlannedArtifact.CreateText("first.A", ""), PlannedArtifact.CreateText("first.z", "")];
        var first = ArtifactFileHeader.Apply(profile, artifacts, syntaxes);
        var second = ArtifactFileHeader.Apply(profile, [.. artifacts.Reverse()], syntaxes);
        first.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Severity, diagnostic.Message)).ShouldEqual(second.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Severity, diagnostic.Message)));
        first.Diagnostics.Length.ShouldEqual(2);
        first.Diagnostics[0].Message.ShouldContain("first.A");
        first.Diagnostics[1].Message.ShouldContain("first.z");
        first.Artifacts.ShouldBeEmpty();
    }
}
