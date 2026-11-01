// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_compliance_details_are_not_specified : compliance_concepts
{
    bool _defaultMatchesExplicitOn;
    RenderedFile _reasonPersonal = null!;

    void Because()
    {
        _reasonPersonal = ConceptRenderer.Render(_applicationSet.Concepts["ReasonPersonal"], _applicationSet, "Generated");
        _defaultMatchesExplicitOn = _applicationSet.Concepts.All(entry =>
            ConceptRenderer.Render(entry.Value, _applicationSet, "Generated").Content == ConceptRenderer.Render(entry.Value, _applicationSet, "Generated", complianceDetails: true).Content);
    }

    [Fact] void should_emit_compliance_details_by_default() => _reasonPersonal.Content.ShouldContain("[ComplianceDetails(\"A contact address.\")]");
    [Fact] void should_render_the_same_output_as_explicitly_enabled_details() => _defaultMatchesExplicitOn.ShouldBeTrue();
    [Fact] void should_enable_details_on_the_default_renderer() => CratisRenderer.CreateDefault().ComplianceDetails.ShouldBeTrue();
}
