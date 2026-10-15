// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap.given;

public class a_gap : for_ArtifactRenderPlan.given.an_artifact_render_request
{
    protected ArtifactImplementationGap _gap = null!;
    protected ArtifactImplementationStub _stub = null!;
    protected ImmutableArray<PlannedArtifact> _artifacts;

    void Establish()
    {
        _stub = new(
            "Features/RegisterPayloads.cs",
            "Projects.RegisterPayloads",
            "Register_Registered_1",
            "static global::Projects.Registered Register_Registered_1(global::Projects.RegisterContext context)",
            new(120, 80),
            new("Projects.RegisterContext", [new("Command", "Projects.Register", true), new("Tenant", "TenantId", false)]),
            ["Projects.Specs.for_Register.when_registering"]);
        _gap = new(
            ArtifactImplementationGapOrigin.Described,
            "requirement-1",
            _request.Model.Application.Id.ToString(),
            SemanticImplementationRole.CommandHandler,
            ["Compute the event payload."],
            1,
            1,
            [_stub]);
        _artifacts = [PlannedArtifact.CreateText(_stub.RelativePath, new string(' ', 256)), PlannedArtifact.CreateText("other.cs", new string(' ', 256))];
    }

    protected string Digest(ImmutableArray<ArtifactImplementationGap> gaps) => ArtifactRenderPlan.Create(_request, _artifacts, [], gaps).Digest;
}
