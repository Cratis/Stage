// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_reporting_distinct_production_refusals : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    ArtifactRenderDiagnostic[] _diagnostics = [];

    void Because()
    {
        var located = _context.SelectedSlices().Single(slice => slice.Slice.Name == "Start");
        var command = located.Slice.Commands.Single();
        var produced = command.Produces.Single();
        var slice = located.Slice with
        {
            Commands = [command with { Destination = null, Produces = [produced with { Destination = null }, produced with { Mappings = [] }] }]
        };
        _diagnostics = [.. SemanticCratisAdmission.Evaluate(_context, [located with { Slice = slice }])];
    }

    [Fact] void should_report_the_destinationless_production() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-053");
    [Fact] void should_still_report_the_other_productions_invalid_mappings() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-006");
}
