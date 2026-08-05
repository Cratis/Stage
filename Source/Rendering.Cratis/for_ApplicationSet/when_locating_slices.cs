// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ApplicationSet.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ApplicationSet;

public class when_locating_slices : an_application_with_a_nested_feature
{
    LocatedSlice _located = null!;

    void Because() => _located = _applicationSet.Slices.Single();

    [Fact] void should_find_the_slice() => _located.Slice.ShouldEqual(_registerSlice);
    [Fact] void should_capture_the_full_module_and_feature_path() => _located.Path.ShouldContainOnly("Billing", "Invoices", "Details");
}
