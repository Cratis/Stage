// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValueShapeAdmission.given;

public class a_value_shape : Specification
{
    private protected SemanticApplicationContext _context = null!;
    protected SemanticTypeReference _type = null!;

    void Establish()
    {
        var model = invoice_model.Compile("""
            concept Count : Int
              validate
                > 5
            type Node
              count Count optional
              next Node optional
              children Node[] optional
            module Inputs
              feature Checking
                slice StateChange Check
                  command CheckInput
                    value Node optional
                    returns value
            """);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var options = new CratisRenderingOptions("Inputs", "Inputs");
        var request = new ArtifactRenderRequest(model, plan, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, options);
        _type = plan.Commands.Values.Single().Properties.Single().Type;
    }
}
