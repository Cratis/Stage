// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_version_profiles;

public class and_a_production_route_changes : Specification
{
    ExecutableSemanticModel _expected;
    ExecutableSemanticModel _changed;

    void Establish()
    {
        _expected = round_trip_fixtures.Compile("""
            concept AccountId : Uuid
            eventsource Banking
              identifier AccountId
              stream Accounts
              stream Archived
            module Banking
              feature Deposits
                slice StateChange Deposit
                  command Deposit
                    accountId AccountId identifier
                    amount Decimal
                    stream Banking.Accounts
                    produces event Deposited
                      stream Banking.Accounts
                      amount Decimal = amount
            """);
        var stream = _expected.Application.EventSources.Single().Streams.Single(stream => stream.Name == "Archived").Id;
        var application = comparison_profile_models.Commands(_expected.Application, command => command with
        {
            Produces = [.. command.Produces.Select(produced => produced with { Route = produced.Route! with { Stream = stream } })]
        });
        _changed = comparison_profile_models.Calendar(ExecutableSemanticModel.Create(_expected.LanguageVersion, _expected.SemanticVersion, application));
    }

    [Fact]
    void should_not_erase_the_stream_change_when_promoting()
    {
        _changed.SemanticVersion.ShouldEqual(SemanticVersion.V37);
        var comparison = round_trip_model_comparison.Compare(_expected, _changed);
        comparison.IsEquivalent.ShouldBeFalse();
        Assert.Contains(comparison.Differences, difference => difference.Path.EndsWith("/route/stream", StringComparison.Ordinal));
    }
}
#endif
