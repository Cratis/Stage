// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_duplicate_command_names : a_register_project_render_request
{
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Because()
    {
        var context = new SemanticApplicationContext(_request);
        var original = context.Slice(_registerProject.Id);
        var duplicate = original with
        {
            Slice = _registerProject with { Name = "RegisterAgain" },
            Path = [.. original.Path.SkipLast(1), "RegisterAgain"]
        };
        var differentCase = original with
        {
            Slice = _registerProject with
            {
                Name = "RegisterWithDifferentCase",
                Commands = [_registerProject.Commands[0] with { Name = "registerproject" }]
            },
            Path = [.. original.Path.SkipLast(1), "RegisterWithDifferentCase"]
        };
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [original, duplicate, differentCase]);
    }

    [Fact] void should_report_one_diagnostic_for_the_duplicate_name() => _diagnostics.Select(_ => _.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_block_rendering() => _diagnostics.Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_identify_every_affected_slice_regardless_of_case() => _diagnostics.Single().Message.ShouldEqual("Command 'RegisterProject' is declared in multiple slices (RegisterProject, RegisterAgain, RegisterWithDifferentCase) within the same feature. Commands must have unique names to avoid route ambiguity.");
}
