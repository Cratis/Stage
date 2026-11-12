// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Http.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpAbilityBackend;

public class when_the_request_does_not_complete : an_http_system
{
    AbilityResult _timedOut = null!;
    AbilityResult _refused = null!;
    AbilityResult _cancelled = null!;
    Exception? _cancellation;

    async Task Establish()
    {
        await StartSystem();
        StartStage(Parsed(BookPickup));
    }

    async Task Because()
    {
        _answer = _ =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return (201, """{"tracking":"late"}""");
        };
        _timedOut = await _backend.Invoke(Call(timeout: TimeSpan.FromMilliseconds(200)), CancellationToken.None);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        _cancellation = await Catch.Exception(async () => _cancelled = await _backend.Invoke(Call(), source.Token));

        _values[$"{System}:Http:BaseAddress"] = "http://127.0.0.1:1/";
        _refused = await _backend.Invoke(Call(), CancellationToken.None);
    }

    [Fact] void should_fail_a_timeout_leaving_the_effect_open() => ((AbilityFailed)_timedOut).MayHaveReachedTheSystem.ShouldBeTrue();
    [Fact] void should_fail_a_refused_connection_leaving_the_effect_open() => ((AbilityFailed)_refused).MayHaveReachedTheSystem.ShouldBeTrue();
    [Fact] void should_not_report_an_address_in_the_failure() => ((AbilityFailed)_refused).Reason.Contains("127.0.0.1", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_let_a_cancellation_through() => (_cancellation is OperationCanceledException).ShouldBeTrue();
}
