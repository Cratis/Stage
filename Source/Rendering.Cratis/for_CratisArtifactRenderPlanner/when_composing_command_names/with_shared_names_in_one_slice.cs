// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_shared_names_in_one_slice : given.a_two_module_request
{
    SemanticApplicationContext _context = null!;
    LocatedSemanticSlice[] _slices = null!;
    readonly List<ArtifactRenderDiagnostic> _diagnostics = [];

    void Establish()
    {
        DuplicateCommandName();
        _context = new SemanticApplicationContext(_request, _options);
        var original = _context.AllSlices.Single(slice => slice.Slice.Id == _registerProject.Id);
        var additional = _context.AllSlices.Single(slice => slice.Module.Name == "OtherProjects").Slice.Commands.Single();

        // The semantic contract already rejects this shape; exercise the renderer's defensive check directly.
        _slices = [original with { Slice = original.Slice with { Commands = [.. original.Slice.Commands, additional] } }];
    }

    void Because() => AmbiguousSceneCommands.Refuse(_context, _slices, null, _diagnostics);

    [Fact] void should_report_an_error_for_the_collision() => _diagnostics.Exists(diagnostic => diagnostic.Code == "STAGE-SCREEN-008" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_name_the_slice_and_command() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").Message.ShouldContain("'RegisterProject' in slice 'Projects.Registration.RegisterProject'");
}
