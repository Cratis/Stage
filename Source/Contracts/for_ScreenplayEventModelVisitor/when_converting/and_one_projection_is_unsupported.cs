// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.for_ReadModelConverter.given;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ScreenplayEventModelVisitor.when_converting;

public class and_one_projection_is_unsupported : a_slice_with_projections
{
    EventModelConversion _result = null!;

    void Because() => _result = new ScreenplayEventModelVisitor().Convert(_application);

    [Fact] void should_return_every_slice() => Slices().Length.ShouldEqual(2);
    [Fact] void should_keep_the_unsupported_read_model() => Slices()[0].ReadModel!.UnconvertedProjectionName.ShouldEqual("ItemsProjection");
    [Fact] void should_convert_the_other_projection() => Slices()[1].ReadModel!.Projection.ShouldNotBeNull();
    [Fact] void should_report_the_owning_slice() => _result.Diagnostics.Single().Path.ShouldEqual("Store.Browsing.Listing");

    Slice[] Slices() => [.. _result.Model.Collections.SelectMany(collection => collection.Modules).SelectMany(module => module.Features).SelectMany(feature => feature.Slices)];
}
