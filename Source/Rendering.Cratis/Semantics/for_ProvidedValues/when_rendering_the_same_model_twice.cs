// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_rendering_the_same_model_twice : Specification
{
    byte[] _first = [];
    byte[] _second = [];

    void Because()
    {
        _first = System.Text.Encoding.UTF8.GetBytes(a_fleet_model.Text(a_fleet_model.Plan(an_available_fleet_model.Source), "Availability.cs"));
        _second = System.Text.Encoding.UTF8.GetBytes(a_fleet_model.Text(a_fleet_model.Plan(an_available_fleet_model.Source), "Availability.cs"));
    }

    [Fact] void should_render_something() => Assert.NotEmpty(_first);
    [Fact] void should_render_identical_bytes() => Assert.Equal(_first, _second);
}
#endif
