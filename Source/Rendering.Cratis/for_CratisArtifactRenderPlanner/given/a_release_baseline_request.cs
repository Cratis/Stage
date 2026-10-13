// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_release_baseline_request : a_register_project_render_request
{
    protected ArtifactRenderPlan _plan = null!;
    protected ArtifactRenderPlan _unchecked = null!;
    protected ReleaseBaselineAssessment _assessment = null!;

    void Establish()
    {
        _unchecked = _planner.Plan(_request);
        var contract = _registerProject.Events.Single();
        _assessment = new(
            ReleaseBaselineAssessmentStatus.Checked,
            _model.Revision,
            [
                new("BaselinedRevisionChanged", contract.ContractId, 1, contract.Name, new ModelNotesAddress("Event", [new("EventContract", contract.Name)]), "v1.0", "$.properties[0].type.primitive"),
                new("BaselinedRevisionMissing", contract.ContractId, 2, contract.Name, new ModelNotesAddress("Event", [new("EventContract", contract.Name)]), "v2.0", null)
            ]);
    }

    protected ArtifactRenderRequest WithBaseline(ReleaseBaselineAssessment assessment) => _request with { ReleaseBaseline = assessment };
}
#endif
