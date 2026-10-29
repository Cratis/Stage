// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance;

public class and_optional_text_is_null_versus_empty : given.a_provenance_request
{
    string _nullLanguage = null!;
    string _emptyLanguage = null!;
    string _nullFile = null!;
    string _emptyFile = null!;
    string _nullDigest = null!;
    string _emptyDigest = null!;

    void Because()
    {
        _nullLanguage = Implementation(language: null, file: "f").Digest;
        _emptyLanguage = Implementation(language: string.Empty, file: "f").Digest;
        _nullFile = Implementation(language: "l", file: null).Digest;
        _emptyFile = Implementation(language: "l", file: string.Empty).Digest;
        _nullDigest = Component(null).Digest;
        _emptyDigest = Component(string.Empty).Digest;
    }

    [Fact] void should_distinguish_a_null_language() => _nullLanguage.ShouldNotEqual(_emptyLanguage);
    [Fact] void should_distinguish_a_null_file() => _nullFile.ShouldNotEqual(_emptyFile);
    [Fact] void should_distinguish_a_null_component_digest() => _nullDigest.ShouldNotEqual(_emptyDigest);

    static ArtifactRenderInputProvenance Implementation(string? language, string? file) =>
        new([], [new("r1", Cratis.Screenplay.Semantics.SemanticImplementationRole.CommandHandler, Cratis.Screenplay.Semantics.SemanticAddress.ForApplication(Cratis.Screenplay.Semantics.ApplicationIdentity.Create("Projects")), language, file, "aa")], []);

    static ArtifactRenderInputProvenance Component(string? digest) =>
        new([], [], [new(ArtifactRenderComponentKind.Kit, "acme", "1.0.0", digest)]);
}
