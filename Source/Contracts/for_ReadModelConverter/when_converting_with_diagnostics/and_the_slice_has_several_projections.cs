// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ReadModelConverter.when_converting_with_diagnostics;

public class and_the_slice_has_several_projections : given.a_slice_with_projections
{
    ReadModelDefinition? _result;

    void Establish()
    {
        var first = _slice.Projections.Single() with { Sequence = null };
        _slice = _slice with { Projections = [first, first with { Name = "Additional" }] };
    }

    void Because() => _result = ReadModelConverter.Convert(_slice, _schema, new Dictionary<string, string>(), "Listing", _diagnostics);

    [Fact] void should_keep_the_first_projection() => _result!.Projection.ShouldNotBeNull();
    [Fact] void should_keep_the_additional_projection_name() => _result!.IgnoredProjectionNames.Single().ShouldEqual("Additional");
    [Fact] void should_report_a_warning() => _diagnostics.All[0].Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_omitted_projection() => _diagnostics.All[0].Construct.ShouldEqual("additional projection 'Additional'");
    [Fact] void should_use_the_additional_projection_code() => _diagnostics.All[0].Code.ShouldEqual("STAGE-PROJECTION-008");
    [Fact] void should_not_mark_the_model_as_unexecutable() => _diagnostics.HasErrors.ShouldBeFalse();
}
