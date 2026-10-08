// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_creating_input_with_command_and_read_model_documentation : Specification
{
    SemanticCompilation _compilation = null!;
    AuthoringMetadataInput.Catalog? _catalog;
    bool _accepted;

    void Establish() => _compilation = when_rendering_authoring_documentation.Compile(
        when_rendering_authoring_documentation.Source);

    void Because()
    {
        var input = AuthoringMetadataInput.Create(_compilation);
        input.ShouldNotBeNull();
        _accepted = AuthoringMetadataInput.TryRead(input!, _compilation.Model, out _catalog);
    }

    [Fact] void should_admit_the_metadata_it_creates() => _accepted.ShouldBeTrue();
    [Fact] void should_key_command_documentation_by_its_semantic_identity() => _catalog!.Entries[Command().Id.ToString()].ShouldEqual(new AuthoringMetadataInput.Metadata("Registers <project> & name", "# Command <notes> & details\n\n- **Keep** the requested name."));
    [Fact] void should_key_read_model_documentation_by_its_semantic_identity() => _catalog!.Entries[ReadModel().Id.ToString()].ShouldEqual(new AuthoringMetadataInput.Metadata("Shows <project> & name", "# View <notes> & details\n\n- `name` may contain <markup> & text."));

    SemanticCommand Command() => _compilation.Model.Application.Modules.Single().Features.Single().Slices[0].Commands.Single();
    SemanticReadModel ReadModel() => _compilation.Model.Application.Modules.Single().Features.Single().Slices[^1].ReadModels.Single();
}
