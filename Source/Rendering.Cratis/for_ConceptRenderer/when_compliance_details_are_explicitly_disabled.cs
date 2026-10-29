// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_compliance_details_are_explicitly_disabled : compliance_concepts
{
    bool _explicitOffBytesUnchanged;

    void Because()
    {
        var comparisons = _applicationSet.Concepts.Values.Where(concept => concept.Name is not ("NamespaceSecret" or "GlobalSecret")).Select(concept =>
        {
            var withoutDetails = concept with
            {
                Attributes = [.. concept.Attributes.Select(attribute => attribute with { Reason = null, Scope = null, SpecialCategory = null, Criminal = false })]
            };
            var baseline = Encoding.UTF8.GetBytes(ConceptRenderer.Render(withoutDetails, _applicationSet, "Generated").Content);
            var explicitOff = Encoding.UTF8.GetBytes(ConceptRenderer.Render(concept, _applicationSet, "Generated", complianceDetails: false).Content);
            return baseline.SequenceEqual(explicitOff);
        }).ToArray();
        _explicitOffBytesUnchanged = comparisons.Length == 17 && comparisons.All(unchanged => unchanged);
    }

    [Fact] void should_preserve_explicitly_disabled_bytes_except_for_non_subject_secrets() => _explicitOffBytesUnchanged.ShouldBeTrue();
    [Fact] void should_allow_the_default_renderer_to_opt_out() => CratisRenderer.CreateDefault(complianceDetails: false).ComplianceDetails.ShouldBeFalse();
}
