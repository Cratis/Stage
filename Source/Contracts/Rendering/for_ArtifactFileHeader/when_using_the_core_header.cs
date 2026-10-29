// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactFileHeader;

public class when_using_the_core_header : Specification
{
    static ArtifactRenderProfile Profile(params ArtifactRenderInput[] inputs) => ArtifactRenderProfile.Create("test", "1", "test", "1", [.. inputs]);
    static ArtifactFileHeaderResult Apply(string path, params string[] lines) => ArtifactFileHeader.Apply(ArtifactFileHeader.With(Profile(), [.. lines]), [PlannedArtifact.CreateText(path, "code")], ArtifactCommentSyntaxes.Default);

    [Fact] void should_encode_the_canonical_json() => ArtifactFileHeader.CreateInput(["Ærlig ©", ""]).Bytes.ShouldEqual(JsonSerializer.SerializeToUtf8Bytes(new[] { "Ærlig ©", "" }));
    [Fact] void should_have_a_stable_hash() => ArtifactFileHeader.CreateInput(["hello"]).Sha256.ShouldEqual(ArtifactFileHeader.CreateInput(["hello"]).Sha256);
    [Fact] void should_leave_an_empty_header_profile_unchanged()
    {
        var profile = Profile();
        ArtifactFileHeader.With(profile, []).ShouldEqual(profile);
    }
    [Fact] void should_allow_tabs_and_unicode() => ArtifactFileHeader.IsValid(["Ærlig ©\t"]).ShouldBeTrue();

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [InlineData("\u0007")]
    [InlineData("\uE000")]
    [InlineData("\U000F0000")]
    [InlineData("\u202E")]
    void should_refuse_unsafe_lines(string line) => ArtifactFileHeader.IsValid([line]).ShouldBeFalse();

    [Fact] void should_refuse_a_null_line() => ArtifactFileHeader.IsValid([null!]).ShouldBeFalse();

    [Fact] void should_refuse_a_lone_surrogate() => ArtifactFileHeader.IsValid([new string((char)0xD800, 1)]).ShouldBeFalse();

    [Theory]
    [InlineData("stage:file-header")]
    [InlineData("cratis-render:file-header")]
    [InlineData("axon-render:file-header")]
    void should_read_every_name(string name)
    {
        var input = ArtifactFileHeader.CreateInput(["hello"]);
        ArtifactFileHeader.TryRead(Profile(ArtifactRenderInput.Create(name, "1", input.Bytes)), out var lines).ShouldBeTrue();
        lines.ShouldEqual(new[] { "hello" });
    }

    [Theory]
    [InlineData("not json", "1")]
    [InlineData("[\"hello\"] ", "1")]
    [InlineData("[]", "1")]
    [InlineData("[null]", "1")]
    [InlineData("[\"hello\"]", "2")]
    void should_refuse_malformed_inputs(string json, string version)
    {
        var result = ArtifactFileHeader.Apply(Profile(ArtifactRenderInput.Create(ArtifactFileHeader.InputName, version, [.. Encoding.UTF8.GetBytes(json)])), [], ArtifactCommentSyntaxes.Default);
        result.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
        result.Artifacts.ShouldBeEmpty();
    }

    [Fact] void should_refuse_duplicate_inputs()
    {
        var input = ArtifactFileHeader.CreateInput(["hello"]);
        ArtifactFileHeader.Apply(Profile(input, ArtifactRenderInput.Create("cratis-render:file-header", "1", input.Bytes)), [], ArtifactCommentSyntaxes.Default).Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-001");
        Catch.Exception(() => ArtifactFileHeader.With(Profile(input), ["hello"])).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    }

    [Theory]
    [InlineData("see \\u000a here")]
    [InlineData("\\uE000")]
    [InlineData("\\uuuu0041")]
    void should_refuse_java_unicode_escapes_only_for_java(string line)
    {
        var refused = Apply("Code.java", line);
        refused.Diagnostics.Single().Code.ShouldEqual("STAGE-HEADER-002");
        refused.Artifacts.ShouldBeEmpty();
        Apply("Code.cs", line).Refused.ShouldBeFalse();
        Apply("Code.kt", line).Refused.ShouldBeFalse();
    }

    [Fact] void should_not_infer_ownership_from_content() => Encoding.UTF8.GetString(ArtifactFileHeader.Apply(ArtifactFileHeader.With(Profile(), ["hello"]), [PlannedArtifact.CreateText("Code.cs", "// hello\n\ncode")], ArtifactCommentSyntaxes.Default).Artifacts.Single().Bytes.AsSpan()).ShouldEqual("// hello\n\n// hello\n\ncode");

    [Fact] void should_apply_once_explicitly()
    {
        var profile = ArtifactFileHeader.With(Profile(), ["hello", "  ", "end  "]);
        var first = ArtifactFileHeader.Apply(profile, [PlannedArtifact.CreateText("Code.cs", "code")], ArtifactCommentSyntaxes.Default);
        var second = ArtifactFileHeader.Apply(profile, first.Artifacts, ArtifactCommentSyntaxes.Default);
        second.Artifacts.Single().ShouldEqual(first.Artifacts.Single());
        Encoding.UTF8.GetString(second.Artifacts.Single().Bytes.AsSpan()).ShouldEqual("// hello\n//\n// end\n\ncode");
    }

    [Theory]
    [InlineData(".cs")][InlineData(".ts")][InlineData(".tsx")][InlineData(".mts")][InlineData(".cts")]
    [InlineData(".js")][InlineData(".jsx")][InlineData(".mjs")][InlineData(".cjs")]
    [InlineData(".kt")][InlineData(".kts")][InlineData(".go")][InlineData(".java")]
    void should_recognize_code_case_insensitively(string extension) => ArtifactCommentSyntaxes.Default.TryGet("dir.with.dot/Code" + extension.ToUpperInvariant(), out _).ShouldBeTrue();

    [Theory]
    [InlineData(".json")][InlineData(".md")][InlineData(".csproj")][InlineData(".props")]
    [InlineData(".yml")][InlineData(".css")][InlineData(".html")]
    void should_leave_content_untouched(string extension)
    {
        var artifact = PlannedArtifact.CreateText("Code" + extension, "code");
        ArtifactFileHeader.Apply(ArtifactFileHeader.With(Profile(), ["hello"]), [artifact], ArtifactCommentSyntaxes.Default).Artifacts.Single().ShouldEqual(artifact);
    }

    [Fact] void should_not_change_the_default_syntaxes()
    {
        var custom = ArtifactCommentSyntaxes.Default.With(".py", new("#", [])).With(".cs", new("!", []));
        custom.TryGet("code.py", out var python).ShouldBeTrue();
        python.LinePrefix.ShouldEqual("#");
        custom.TryGet("code.cs", out var changed).ShouldBeTrue();
        changed.LinePrefix.ShouldEqual("!");
        ArtifactCommentSyntaxes.Default.TryGet("code.py", out _).ShouldBeFalse();
        ArtifactCommentSyntaxes.Default.TryGet("code.cs", out var original).ShouldBeTrue();
        original.LinePrefix.ShouldEqual("//");
    }
}
