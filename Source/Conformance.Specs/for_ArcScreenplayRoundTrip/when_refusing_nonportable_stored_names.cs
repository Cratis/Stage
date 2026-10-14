// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_refusing_nonportable_stored_names : given.a_rendered_application
{
    protected override string Source => StoredNames("stored\\\"account\\\\source", "stored\\\"transactions\\\\stream");
    void Because() => Generate();

    [Fact] void should_report_the_unreadable_route() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.UnreadableCommandRoute);
    [Fact] void should_report_the_source_omission() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.EventSourceNotRepresentable);
    [Fact] void should_not_invent_a_source_definition() => _recovered.Application.EventSources.ShouldBeEmpty();
    [Fact] void should_preserve_the_exact_stored_source_in_the_analyzed_binding() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().EventSource!.StoredSource.ShouldEqual("stored\"account\\source");
    [Fact] void should_preserve_the_exact_stored_stream_in_the_analyzed_binding() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().EventSource!.StoredStream.ShouldEqual("stored\"transactions\\stream");
}
#endif
