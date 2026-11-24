// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_a_source_contract_is_invalid : Specification
{
    [Theory]
    [InlineData("pair", false)]
    [InlineData("pair", true)]
    [InlineData("graph", false)]
    [InlineData("graph", true)]
    [InlineData("future", false)]
    [InlineData("future", true)]
    void should_fail_conversion_on_either_side(string invalidity, bool reverse)
    {
        var valid = comparison_profile_models.Legacy;
        var application = invalidity == "graph" ? valid.Application with { Modules = [valid.Application.Modules[0], valid.Application.Modules[0]] } : valid.Application;
        var language = invalidity == "future" ? new LanguageVersion(999, 0) : valid.LanguageVersion;
        var semantic = invalidity switch { "pair" => SemanticVersion.V2, "future" => new SemanticVersion(999, 0), _ => valid.SemanticVersion };
        var invalid = comparison_profile_models.Unchecked(valid, language, semantic, application);

        Assert.Throws<InvalidSemanticContract>(() => reverse ? round_trip_model_comparison.Compare(invalid, valid) : round_trip_model_comparison.Compare(valid, invalid));
    }
}
#endif
