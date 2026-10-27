// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_default_scalar_values : a_populated_model
{
    void Because()
    {
        _known.Remove("SemanticProperty.IsGenerated");
        _known.Remove("LanguageVersion.Minor");
        _unknown = SemanticUnknownMembers.Find(_model, _known);
    }

    [Fact] void should_treat_false_as_populated() => _unknown.Any(member => member.Member == "SemanticProperty.IsGenerated").ShouldBeTrue();
    [Fact] void should_treat_zero_as_populated() => _unknown.Any(member => member.Member == "LanguageVersion.Minor").ShouldBeTrue();
    [Fact] void should_not_report_static_version_constants() => _unknown.Any(member => member.Member == "LanguageVersion.V25").ShouldBeFalse();
}
