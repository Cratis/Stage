// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_recovering_a_filtered_reactor : given.a_rendered_application
{
    protected override string Source => StoredNames("stored-account", "stored-transactions") + """

            slice Automation Forward
              event Forwarded
                amount Decimal
              reaction Follow
                from Account.Transactions
                when Deposited
                  produces Forwarded
                    amount = amount
        """;
    void Because() => Generate();

    [Fact] void should_recover_the_source_and_stream_names_and_ids() => AssertSourceAndStreamsRecovered();
    [Fact] void should_recover_the_observed_source() => _result.Model.Slices.SelectMany(slice => slice.Reactors).Single().EventSource!.Source.ShouldEqual("Account");
    [Fact] void should_recover_the_observed_stream() => _result.Model.Slices.SelectMany(slice => slice.Reactors).Single().EventSource!.Stream.ShouldEqual("Transactions");
    [Fact] void should_recognize_the_observed_stream_as_declared() => _result.Model.Slices.SelectMany(slice => slice.Reactors).Single().EventSource!.StreamDeclared.ShouldBeTrue();
    [Fact] void should_not_report_source_or_stream_diagnostics() => AssertSourceDiagnosticsAbsent();
}
#endif
