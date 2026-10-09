// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_compliance_details_are_not_enabled : compliance_concepts
{
    bool _defaultBytesUnchanged;
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
            var defaults = Encoding.UTF8.GetBytes(ConceptRenderer.Render(concept, _applicationSet, "Generated").Content);
            var explicitOff = Encoding.UTF8.GetBytes(ConceptRenderer.Render(concept, _applicationSet, "Generated", complianceDetails: false).Content);
            return (Default: baseline.SequenceEqual(defaults), Off: baseline.SequenceEqual(explicitOff));
        }).ToArray();
        _defaultBytesUnchanged = comparisons.Length == 17 && comparisons.All(comparison => comparison.Default);
        _explicitOffBytesUnchanged = comparisons.Length == 17 && comparisons.All(comparison => comparison.Off);
    }

    [Fact] void should_preserve_default_bytes_except_for_non_subject_secrets() => _defaultBytesUnchanged.ShouldBeTrue();
    [Fact] void should_preserve_explicitly_disabled_bytes_except_for_non_subject_secrets() => _explicitOffBytesUnchanged.ShouldBeTrue();
    [Fact] void should_keep_the_default_renderer_option_off() => CratisRenderer.CreateDefault().ComplianceDetails.ShouldBeFalse();
    [Fact] void should_allow_the_default_renderer_to_opt_in() => CratisRenderer.CreateDefault(complianceDetails: true).ComplianceDetails.ShouldBeTrue();
}
