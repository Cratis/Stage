// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_recording_input_provenance.given;

public class a_provenance_request : for_ArtifactRenderPlan.given.an_artifact_render_request
{
    protected static ArtifactRenderInput Input(string name, string version, string content) =>
        ArtifactRenderInput.Create(name, version, [.. Encoding.UTF8.GetBytes(content)]);

    protected static SemanticImplementationRequirement Requirement(
        string id,
        SemanticImplementationRole role = SemanticImplementationRole.CommandHandler,
        string owner = "Projects",
        string? language = "csharp",
        string? file = "handler.cs",
        string hash = "aa") =>
        new(role, SemanticAddress.ForApplication(ApplicationIdentity.Create(owner)), "Member", language, file, hash, null!) { RequirementId = id };

    protected ArtifactRenderRequest With(ImmutableArray<ArtifactRenderInput> inputs, ImmutableArray<SemanticImplementationRequirement> requirements) =>
        _request with
        {
            Profile = ArtifactRenderProfile.Create("cratis", "1", "Cratis.Stage.Rendering.Cratis", "1", inputs),
            ImplementationRequirements = requirements
        };

    protected static ArtifactRenderPlan Plan(ArtifactRenderRequest request) =>
        ArtifactRenderPlan.Create(request, [PlannedArtifact.CreateText("same.cs", "same")], []);
}
