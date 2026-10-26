// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationAdmission.when_verifying_an_envelope.given;

public class a_verified_envelope : Specification
{
    protected ArtifactRenderRequest _request = null!;
    protected SemanticImplementationRequirement _requirement = null!;
    protected ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Establish()
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        _requirement = loaded.ImplementationRequirements.Single();
        _request = new(loaded.Model, loaded.Plan, CratisRendering.CreateProfile("InvoiceModel", new("Orders", "Orders")), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            AttachmentDiagnostics = loaded.AttachmentDiagnostics,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
    }
}
#endif
