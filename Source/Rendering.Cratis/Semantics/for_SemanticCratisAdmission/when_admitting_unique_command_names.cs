// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_unique_command_names : a_register_project_render_request
{
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Because()
    {
        var context = new SemanticApplicationContext(_request);
        var original = context.Slice(_registerProject.Id);
        var other = original with
        {
            Slice = _registerProject with
            {
                Name = "RegisterAnotherProject",
                Commands = [_registerProject.Commands[0] with { Name = "RegisterAnotherProject" }]
            },
            Path = [.. original.Path.SkipLast(1), "RegisterAnotherProject"]
        };
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [original, other]);
    }

    [Fact] void should_admit_commands_with_unique_names() => _diagnostics.ShouldBeEmpty();
}
