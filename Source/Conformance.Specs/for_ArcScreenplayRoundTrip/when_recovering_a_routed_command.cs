// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_a_routed_command : given.a_rendered_application
{
    protected override string Source => RoutedSource;
    void Because() => Generate();

    [Fact] void should_recover_the_source_and_its_stream() => AssertSourceAndStreamsRecovered();
    [Fact] void should_recover_the_command_source() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.Source.ShouldEqual(_model.Application.EventSources.Single().Name);
    [Fact] void should_recover_the_command_stream() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.Stream.ShouldEqual(_model.Application.EventSources.Single().Streams.Single().Name);
    [Fact] void should_keep_the_default_stream_id() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.StreamId.ShouldBeNull();
    [Fact] void should_not_report_source_or_stream_diagnostics() => AssertSourceDiagnosticsAbsent();
}
#endif
