// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.given;

public class an_artifact_render_request : Specification
{
    protected ArtifactRenderRequest _request = null!;

    void Establish()
    {
        var applicationIdentity = ApplicationIdentity.Create("Projects");
        var applicationAddress = SemanticAddress.ForApplication(applicationIdentity);
        var application = new SemanticApplication(SemanticId.Create(applicationAddress), "Projects", [], [], []);
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        var executionPlan = SemanticExecutionPlan.Compile(model).Plan!;
        var profile = ArtifactRenderProfile.Create("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", []);
        _request = new(model, executionPlan, profile, new(ArtifactRenderScopeKind.Application, application.Id));
    }
}
