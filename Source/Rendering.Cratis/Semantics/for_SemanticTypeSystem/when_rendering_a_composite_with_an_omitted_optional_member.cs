// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTypeSystem;

public class when_rendering_a_composite_with_an_omitted_optional_member : Specification
{
    SemanticTypeSystem _types = null!;
    SemanticTypeReference _type = null!;
    SemanticValue _value = null!;
    string _rendered = null!;

    void Establish()
    {
        var model = invoice_model.Compile("type Details\n  title String\n  display String optional\nmodule Inputs\n  feature Optional\n    slice StateChange Check\n      command Check\n        details Details");
        var details = model.Application.Types.Single();
        var options = new CratisRenderingOptions("Inputs", "Inputs");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Inputs", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _types = new(new SemanticApplicationContext(request, options));
        _type = SemanticTypeReference.ForCompositeType(details.Id);
        _value = SemanticValue.Composite([new(details.Properties.Single(property => property.Name == "title").Id, SemanticValue.Text("Title"))]);
    }

    void Because() => _rendered = _types.Value(_value, _type);

    [Fact] void should_preserve_present_members_and_supply_null_for_the_omitted_member() => _rendered.ShouldEqual("new global::Inputs.Common.Details(\"Title\", null)");
}
