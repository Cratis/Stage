// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SourceLineMappingInput;

public class when_reading_source_line_mapping_inputs : Specification
{
    static readonly DocumentId _document = DocumentId.Create("Invoices.play");

    ArtifactRenderProfile _plain = null!;
    ArtifactRenderProfile _mapped = null!;
    ArtifactRenderProfile _duplicate = null!;
    ArtifactRenderProfile _wrongBytes = null!;
    ArtifactRenderProfile _wrongVersion = null!;
    SourceLineMappingInput.Mapping? _mapping;
    bool _plainRead;
    bool _mappedRead;

    void Because()
    {
        _plain = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        _mapped = CratisRendering.WithSourceLineMapping(_plain, "Model", Documents("Invoices.play"));
        _duplicate = With(_mapped, ArtifactRenderInput.Create(SourceLineMappingInput.Name, "2", SourceLineMappingInput.Create("", Documents("Other.play")).Bytes));
        _wrongBytes = With(_plain, ArtifactRenderInput.Create(SourceLineMappingInput.Name, SourceLineMappingInput.Version, [.. Encoding.UTF8.GetBytes("[]")]));
        _wrongVersion = With(_plain, ArtifactRenderInput.Create(SourceLineMappingInput.Name, "2", SourceLineMappingInput.Create("", []).Bytes));
        _plainRead = SourceLineMappingInput.TryRead(_plain, out var none) && none is null;
        _mappedRead = SourceLineMappingInput.TryRead(_mapped, out _mapping);
    }

    [Fact] void should_not_carry_the_input_by_default() => _plain.Inputs.Any(input => input.Name == SourceLineMappingInput.Name).ShouldBeFalse();
    [Fact] void should_accept_an_absent_input_with_no_mapping() => _plainRead.ShouldBeTrue();
    [Fact] void should_read_a_valid_input() => _mappedRead.ShouldBeTrue();
    [Fact] void should_join_the_root_and_the_display_path() => _mapping.PathOf(_document).ShouldEqual("Model/Invoices.play");
    [Fact] void should_use_the_display_path_alone_for_an_empty_root() => new SourceLineMappingInput.Mapping(string.Empty, new Dictionary<DocumentId, string> { [_document] = "a.play" }).PathOf(_document).ShouldEqual("a.play");
    [Fact] void should_refuse_a_duplicate_input() => SourceLineMappingInput.TryRead(_duplicate, out _).ShouldBeFalse();
    [Fact] void should_refuse_a_non_canonical_payload() => SourceLineMappingInput.TryRead(_wrongBytes, out _).ShouldBeFalse();
    [Fact] void should_refuse_a_wrong_version() => SourceLineMappingInput.TryRead(_wrongVersion, out _).ShouldBeFalse();
    [Fact] void should_replace_rather_than_duplicate_when_mapping_twice() => CratisRendering.WithSourceLineMapping(_mapped, "", Documents("a.play")).Inputs.Count(input => input.Name == SourceLineMappingInput.Name).ShouldEqual(1);

    [Theory]
    [InlineData("/abs")]
    [InlineData("a\\b")]
    [InlineData("a\"b")]
    [InlineData("a/../b")]
    [InlineData("./a")]
    [InlineData("a//b")]
    [InlineData("C:/a")]
    public void should_refuse_an_invalid_display_path(string path) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => SourceLineMappingInput.Create(string.Empty, Documents(path)));

    [Theory]
    [InlineData("/abs")]
    [InlineData("a\\b")]
    [InlineData("a/./b")]
    [InlineData("a/../b")]
    public void should_refuse_an_invalid_root(string root) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => SourceLineMappingInput.Create(root, Documents("a.play")));

    [Fact] void should_allow_leading_parent_segments_in_the_root() => SourceLineMappingInput.Create("../../Model", Documents("a.play")).Name.ShouldEqual(SourceLineMappingInput.Name);

    static IEnumerable<KeyValuePair<DocumentId, string>> Documents(string path) => [KeyValuePair.Create(_document, path)];

    static ArtifactRenderProfile With(ArtifactRenderProfile profile, ArtifactRenderInput input) =>
        ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, input]);
}
