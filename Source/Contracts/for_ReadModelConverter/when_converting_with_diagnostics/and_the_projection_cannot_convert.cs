// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ReadModelConverter.when_converting_with_diagnostics;

public class and_the_projection_cannot_convert : given.a_slice_with_projections
{
    ReadModelDefinition? _result;

    void Because() => _result = ReadModelConverter.Convert(_slice, _schema, new Dictionary<string, string>(), "Store.Browsing.Listing", _diagnostics);

    [Fact] void should_keep_the_read_model() => _result!.Name.ShouldEqual("Items");
    [Fact] void should_keep_its_schema() => _result!.Schema.ShouldNotBeNull();
    [Fact] void should_omit_the_projection() => _result!.Projection.ShouldBeNull();
    [Fact] void should_record_the_unconverted_projection_name() => _result!.UnconvertedProjectionName.ShouldEqual("ItemsProjection");
    [Fact] void should_preserve_the_selected_name() => _result!.SelectedProjectionName.ShouldEqual("ItemsProjection");
    [Fact] void should_report_the_omission() => _diagnostics.All[0].Code.ShouldEqual("STAGE-PROJECTION-001");
}
