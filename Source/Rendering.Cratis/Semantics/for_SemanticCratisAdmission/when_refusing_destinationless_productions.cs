// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_refusing_destinationless_productions : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    LocatedSemanticSlice _stateChange = null!;
    ArtifactRenderDiagnostic[] _diagnostics = [];

    void Establish()
    {
        var located = _context.SelectedSlices().Single(slice => slice.Slice.Name == "Start");
        var command = located.Slice.Commands.Single();
        var produced = command.Produces.Single() with { Destination = null };
        _stateChange = located with { Slice = located.Slice with { Commands = [command with { Destination = null, Produces = [produced] }] } };
    }

    void Because() => _diagnostics = [.. SemanticCratisAdmission.Evaluate(_context, [_stateChange])];

    [Fact] void should_report_the_unallocated_destination() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-053");
    [Fact] void should_not_report_the_generic_production_refusal() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-006");
    [Fact] void should_name_the_command() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-053").Message.ShouldContain("StartWork");
    [Fact] void should_name_the_event() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-053").Message.ShouldContain("Started");
}
