// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Http.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Http.for_HttpAbilityBackend;

public class when_an_ability_is_answered : an_http_system
{
    AbilityResult _returned = null!;
    AbilityResult _outcome = null!;
    AbilityResult _unmappedServerError = null!;
    AbilityResult _unmappedClientError = null!;
    AbilityResult _redirected = null!;
    RecordedRequest _request = null!;

    async Task Establish()
    {
        await StartSystem();
        StartStage(Parsed(BookPickup));
    }

    async Task Because()
    {
        _returned = await _backend.Invoke(Call(), CancellationToken.None);
        _request = _received.Single();

        _answer = _ => (409, string.Empty);
        _outcome = await _backend.Invoke(Call(), CancellationToken.None);

        _answer = _ => (503, "busy");
        _unmappedServerError = await _backend.Invoke(Call(), CancellationToken.None);

        _answer = _ => (422, "detail-from-the-server");
        _unmappedClientError = await _backend.Invoke(Call(), CancellationToken.None);

        _received.Clear();
        _answer = _ => (302, "elsewhere");
        _redirected = await _backend.Invoke(Call(), CancellationToken.None);
    }

    [Fact] void should_post_to_the_path_under_the_base_address() => (_request.Method + " " + _request.Target).ShouldEqual("POST /pickups");
    [Fact] void should_send_the_idempotency_key_of_the_invocation() => _request.Headers["Idempotency-Key"].ShouldEqual("invocation-1");
    [Fact] void should_send_the_body_as_structured_values() => _request.Body.ShouldEqual("""{"reference":"p-1","street":"Main 1","express":true}""");
    [Fact] void should_return_the_mapped_values() => ((AbilityReturned)_returned).Output.ShouldEqual("""{"trackingNumber":"TRK-1"}""");
    [Fact] void should_return_without_an_outcome_on_success() => ((AbilityReturned)_returned).Outcome.ShouldBeNull();
    [Fact] void should_settle_with_the_outcome_a_status_maps_to() => ((AbilityReturned)_outcome).Outcome.ShouldEqual("NoCapacity");
    [Fact] void should_fail_an_unmapped_server_error_leaving_the_effect_open() => ((AbilityFailed)_unmappedServerError).MayHaveReachedTheSystem.ShouldBeTrue();
    [Fact] void should_fail_an_unmapped_client_error_with_the_effect_ruled_out() => ((AbilityFailed)_unmappedClientError).MayHaveReachedTheSystem.ShouldBeFalse();
    [Fact] void should_not_report_the_body_of_a_failure() => ((AbilityFailed)_unmappedClientError).Reason.Contains("detail-from-the-server", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_follow_a_redirect() => _received.Count.ShouldEqual(1);
    [Fact] void should_fail_a_redirect_it_does_not_map() => (_redirected is AbilityFailed).ShouldBeTrue();
}
