// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_different_stored_names : given.a_rendered_application
{
    protected override string Source => StoredNames("stored-account", "stored-transactions");
    void Because() => Generate();

    [Fact] void should_recover_the_source_and_stream_model_names_and_ids() => AssertSourceAndStreamsRecovered();
    [Fact] void should_recover_the_command_source_model_name() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.Source.ShouldEqual("Account");
    [Fact] void should_recover_the_command_stream_model_name() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.Stream.ShouldEqual("Transactions");
    [Fact] void should_recover_the_command_stored_source() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.StoredSource.ShouldEqual(_model.Application.EventSources.Single().SourceKind);
    [Fact] void should_recover_the_command_stored_stream() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.StoredStream.ShouldEqual(_model.Application.EventSources.Single().Streams.Single().StreamKind);
    [Fact] void should_not_report_source_or_stream_diagnostics() => AssertSourceDiagnosticsAbsent();
}
#endif
