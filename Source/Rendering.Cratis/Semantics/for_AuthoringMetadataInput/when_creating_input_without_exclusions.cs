// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_creating_input_without_exclusions : Specification
{
    SemanticCompilation _ordinary = null!;
    SemanticCompilation _admissible = null!;
    bool _identicalMetadata;
    bool _identicalArtifacts;

    void Establish()
    {
        _ordinary = when_rendering_authoring_documentation.Compile(when_rendering_authoring_documentation.Source);
        var result = new SemanticModelCompiler().CompileAdmissible("Projects", _ordinary.Documents);
        Assert.True(result.Compilation.Success, string.Join(Environment.NewLine, result.Compilation.Diagnostics));
        Assert.Empty(result.Exclusions);
        _admissible = result.Compilation.Value!;
    }

    void Because()
    {
        var ordinary = AuthoringMetadataInput.Create(_ordinary);
        var admissible = AuthoringMetadataInput.Create(_admissible);
        ordinary.ShouldNotBeNull();
        admissible.ShouldNotBeNull();
        _identicalMetadata = ordinary.Bytes.SequenceEqual(admissible.Bytes);
        _identicalArtifacts = when_rendering_authoring_documentation.Plan(_ordinary).Artifacts.Select(artifact => artifact.Sha256)
            .SequenceEqual(when_rendering_authoring_documentation.Plan(_admissible).Artifacts.Select(artifact => artifact.Sha256));
    }

    [Fact] void should_keep_identical_metadata_bytes() => _identicalMetadata.ShouldBeTrue();
    [Fact] void should_keep_identical_artifact_digests() => _identicalArtifacts.ShouldBeTrue();
}
