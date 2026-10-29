// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.given;

public class a_reducer_specification : Specification
{
    protected ArtifactRenderRequest _request = null!;
    protected List<ArtifactRenderDiagnostic> _diagnostics = [];
    protected virtual string Body => "return new Total(context.Event.Id, context.Event.Amount);";

    async Task Establish()
    {
        const string specification = """
                  specification CreatingATotal
                    when PlaceOrder
                      id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      amount = 20
                    then OrderPlaced
                      id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      amount = 20
                    then readmodel Total
                      id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      amount = 20
                slice StateView Totals
            """;
        var source = ("concept OrderId : Uuid\n" + when_rendering_a_pure_reducer.Source)
            .Replace("id Uuid", "id OrderId", StringComparison.Ordinal)
            .Replace("return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);", Body, StringComparison.Ordinal)
            .Replace("    slice StateView Totals", specification, StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        _request = new(
            loaded.Model,
            SemanticExecutionPlan.Compile(loaded.Model).Plan!,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
    }

    protected void Validate()
    {
        var context = new SemanticApplicationContext(_request, new("Projects", "Projects"));
        var slice = context.Application.Modules.Single().Features.Single().Slices[0];
        SemanticSpecificationAdmission.Validate(context, slice, _diagnostics);
    }

    protected string Refusal()
    {
        var diagnostic = _diagnostics.Single();
        diagnostic.Code.ShouldEqual("STAGE-ESM-011");
        diagnostic.Message.ShouldContain("Specification 'CreatingATotal' cannot render: ");
        diagnostic.Message.ShouldContain("Fold");
        diagnostic.Message.ShouldContain("Total");
        diagnostic.Message.ShouldContain("OrderPlaced");
        return diagnostic.Message;
    }
}
