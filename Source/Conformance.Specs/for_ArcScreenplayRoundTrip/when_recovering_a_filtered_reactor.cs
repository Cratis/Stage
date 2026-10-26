// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_a_filtered_reactor : given.a_rendered_application
{
    protected override string Source => round_trip_fixtures.InlineSource(round_trip_fixtures.FilteredReactor);
    void Because() => Generate();

    [Fact] void should_round_trip_to_an_equivalent_model() => AssertRoundTripEquivalent(round_trip_fixtures.FilteredReactor);
    [Fact] void should_recover_the_observed_source() => _result.Model.Slices.SelectMany(slice => slice.Reactors).Single().EventSource!.Source.ShouldEqual("Account");
    [Fact] void should_recover_the_observed_stream() => _result.Model.Slices.SelectMany(slice => slice.Reactors).Single().EventSource!.Stream.ShouldEqual("Transactions");
    [Fact] void should_recognize_the_observed_stream_as_declared() => _result.Model.Slices.SelectMany(slice => slice.Reactors).Single().EventSource!.StreamDeclared.ShouldBeTrue();
    [Fact] void should_not_report_source_or_stream_diagnostics() => AssertSourceDiagnosticsAbsent();
}
#endif
