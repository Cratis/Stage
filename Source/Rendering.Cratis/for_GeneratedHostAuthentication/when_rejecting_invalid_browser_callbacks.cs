// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

[Collection(nameof(offline_authentication_collection))]
public class when_rejecting_invalid_browser_callbacks(OfflineAuthenticationFixture fixture) : Specification
{
    readonly List<bool> _rejected = [];
    readonly List<bool> _issuedSession = [];

    async Task Because()
    {
        using var scenario = fixture.BeginScenario();
        foreach (var failure in Enum.GetValues<BrowserFailure>().Where(failure => failure != BrowserFailure.None))
        {
            await using var host = await OfflineHost.Start(fixture);
            var browser = new OfflineBrowser();
            await browser.Login(host, failure);
            _rejected.Add((int)browser.Status >= 400);
            _issuedSession.Add(browser.Cookie is not null);
        }
    }

    [Fact] void should_reject_invalid_state_correlation_nonce_and_id_tokens_with_framework_validation_enabled() => _rejected.ShouldEqual(Enumerable.Repeat(true, 8));
    [Fact] void should_never_issue_a_cookie_for_a_rejected_callback() => _issuedSession.ShouldEqual(Enumerable.Repeat(false, 8));
}
