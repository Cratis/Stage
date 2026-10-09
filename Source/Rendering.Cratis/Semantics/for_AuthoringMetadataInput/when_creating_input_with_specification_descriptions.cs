// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_creating_input_with_specification_descriptions : Specification
{
    SemanticCompilation _compilation = null!;
    AuthoringMetadataInput.Catalog? _catalog;
    bool _accepted;

    void Establish() => _compilation = when_rendering_authoring_documentation.Compile(
        when_rendering_specification_descriptions.Source.Replace(
            when_rendering_specification_descriptions.Description,
            "Witnesses <registration> & lookup\u0085Next  \u2028Last\u2029End  ",
            StringComparison.Ordinal));

    void Because()
    {
        var input = AuthoringMetadataInput.Create(_compilation);
        input.ShouldNotBeNull();
        _accepted = AuthoringMetadataInput.TryRead(input!, _compilation.Model, out _catalog);
    }

    [Fact] void should_admit_the_metadata_it_creates() => _accepted.ShouldBeTrue();
    [Fact] void should_include_only_the_three_described_specifications() => _catalog!.Entries.Count.ShouldEqual(3);

    [Fact]
    void should_key_normalized_descriptions_by_the_specification_identity()
    {
        foreach (var specification in _compilation.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications))
        {
            _catalog!.Entries[specification.Id.ToString()].ShouldEqual(new AuthoringMetadataInput.Metadata("Witnesses <registration> & lookup\nNext\nLast\nEnd", null));
        }
    }

    [Fact]
    void should_not_create_an_input_for_undescribed_specifications()
    {
        var compilation = when_rendering_authoring_documentation.Compile(when_rendering_specification_descriptions.Source.Replace(
            $"        description \"{when_rendering_specification_descriptions.Description}\"\n", string.Empty, StringComparison.Ordinal));
        AuthoringMetadataInput.Create(compilation).ShouldBeNull();
    }

    [Fact]
    void should_reject_invalid_xml_in_a_specification_description()
    {
        var compilation = when_rendering_authoring_documentation.Compile(when_rendering_specification_descriptions.Source.Replace(
            when_rendering_specification_descriptions.Description, "Witnesses\u0001registration", StringComparison.Ordinal));
        var error = Catch.Exception(() => AuthoringMetadataInput.Create(compilation));
        error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
        error.Message.ShouldContain("contains invalid XML characters");
    }
}
