// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderScopeSelection;

public class when_locating_slices : given.a_scoped_render_request
{
    LocatedSemanticSlice _nested = null!;
    void Because() => _nested = RenderScopeSelection.Locate(_application).Single(slice => slice.Slice.Name == "Save");
    [Fact] void should_keep_the_entire_feature_path() => _nested.Features.Select(feature => feature.Name).ShouldEqual("Accounts", "Savings");
    [Fact] void should_place_the_slice_under_module_features_and_slice_name() => _nested.Path.ShouldEqual("Banking", "Accounts", "Savings", "Save");
    [Fact] void should_keep_the_module() => _nested.Module.Id.ShouldEqual(_banking.Id);
}
