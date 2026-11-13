// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_reordering_the_declarations : Specification
{
    string _original = string.Empty;
    string _reordered = string.Empty;

    void Because()
    {
        _original = availability_manifest_text.From(a_fleet_model.Text(a_fleet_model.Plan(an_available_fleet_model.Source), "Availability.cs"));
        _reordered = availability_manifest_text.From(a_fleet_model.Text(a_fleet_model.Plan(an_available_fleet_model_reordered.Source), "Availability.cs"));
    }

    [Fact] void should_render_a_manifest() => Assert.Contains("AvailabilityGateEntry", _original, StringComparison.Ordinal);
    [Fact] void should_render_the_same_manifest() => Assert.Equal(_original, _reordered);
}
#endif
