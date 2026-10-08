// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_creating_input_with_a_described_reaction : Specification
{
    SemanticCompilation _compilation = null!;
    AuthoringMetadataInput.Catalog? _catalog;
    bool _accepted;

    void Establish() => _compilation = when_rendering_authoring_documentation.Compile(
        when_rendering_authoring_documentation.Source.Replace(
            "    slice StateView ProjectLookup",
            """
                slice Automation NotifyRegistration
                  reaction RegistrationNotification
                    description "Notifies about registration"
                    when ProjectRegistered
                      description "Handles a registered project"
                slice StateView ProjectLookup
            """,
            StringComparison.Ordinal));

    void Because()
    {
        var input = AuthoringMetadataInput.Create(_compilation);
        input.ShouldNotBeNull();
        _accepted = AuthoringMetadataInput.TryRead(input!, _compilation.Model, out _catalog);
    }

    [Fact] void should_admit_the_metadata_it_creates() => _accepted.ShouldBeTrue();
    [Fact] void should_preserve_the_command_description() => _catalog!.Entries.Values.ShouldContain(new AuthoringMetadataInput.Metadata("Registers <project> & name", null));
    [Fact] void should_exclude_the_syntax_only_reaction_description() => _catalog!.Entries.Values.ShouldNotContain(new AuthoringMetadataInput.Metadata("Notifies about registration", null));
    [Fact] void should_exclude_the_syntax_only_trigger_description() => _catalog!.Entries.Values.ShouldNotContain(new AuthoringMetadataInput.Metadata("Handles a registered project", null));
}
