// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_creating_authoring_documentation_with_invalid_xml : Specification
{
    SemanticCompilation _compilation = null!;
    Exception? _error;

    void Establish() => _compilation = when_rendering_authoring_documentation.Compile(
        when_rendering_authoring_documentation.Source.Replace(
            "Registers <project> & name", "Registers\u0001project", StringComparison.Ordinal));

    void Because() => _error = Catch.Exception(() => CratisRendering.WithAuthoringMetadata(
        CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), _compilation));

    [Fact] void should_report_a_malformed_render_contract() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_explain_that_the_metadata_contains_invalid_xml_characters() => _error!.Message.ShouldContain("contains invalid XML characters");
}
