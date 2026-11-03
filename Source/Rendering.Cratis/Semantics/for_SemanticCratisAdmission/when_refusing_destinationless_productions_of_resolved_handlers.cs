// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_refusing_destinationless_productions_of_resolved_handlers : Specification
{
    ArtifactRenderDiagnostic[] _resolved = [];
    ArtifactRenderDiagnostic[] _pending = [];

    void Because()
    {
        _resolved = Evaluate(handler_model.Request());
        var request = handler_model.Request();
        _pending = Evaluate(request with { ImplementationRequirements = [.. request.ImplementationRequirements.Select(requirement => requirement with { AttachmentResolution = SemanticAttachmentResolution.Pending })] });
    }

    [Fact] void should_report_the_unallocated_destination_of_a_resolved_csharp_handler() => _resolved.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-053");
    [Fact] void should_not_report_the_unallocated_destination_of_a_pending_handler() => _pending.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-053");
    [Fact] void should_not_report_the_production_shape_of_a_pending_handler() => _pending.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-ESM-006");

    static ArtifactRenderDiagnostic[] Evaluate(ArtifactRenderRequest request)
    {
        var context = new SemanticApplicationContext(request, new("HandlerApp", "Orders"));
        var located = context.SelectedSlices().Single();
        var command = located.Slice.Commands.Single();
        var produced = command.Produces.Single() with { Destination = null };
        var stateChange = located with { Slice = located.Slice with { Commands = [command with { Destination = null, Produces = [produced] }] } };

        return [.. SemanticCratisAdmission.Evaluate(context, [stateChange])];
    }
}
