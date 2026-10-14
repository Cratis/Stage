// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_a_source_name_ending_in_EventSource : given.a_rendered_application
{
    protected override string Source => RoutedSource.Replace("Account", "AccountEventSource", StringComparison.Ordinal);
    void Because() => Generate();

    [Fact] void should_preserve_the_model_suffix_on_the_source() => AssertSourceAndStreamsRecovered();
    [Fact] void should_preserve_the_model_suffix_on_the_command_route() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.Source.ShouldEqual("AccountEventSource");
    [Fact] void should_preserve_the_stream_name() => _result.Model.Slices.SelectMany(slice => slice.Commands).Single().Authoring!.Route!.Stream.ShouldEqual("Transactions");
    [Fact] void should_not_report_source_or_stream_diagnostics() => AssertSourceDiagnosticsAbsent();
}
#endif
