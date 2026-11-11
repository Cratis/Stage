// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_ExtensionSessions;

public class when_issuing_credentials : Specification
{
    ExtensionSessions _sessions = null!;
    string _first = null!;
    string _again = null!;
    string _other = null!;

    void Because()
    {
        _sessions = new ExtensionSessions();
        _first = _sessions.TokenFor("Slack");
        _again = _sessions.TokenFor("Slack");
        _other = _sessions.TokenFor("Mail");
    }

    [Fact] void should_issue_the_same_credential_for_a_system_every_time() => _again.ShouldEqual(_first);
    [Fact] void should_issue_another_credential_for_another_system() => _other.ShouldNotEqual(_first);
    [Fact] void should_find_the_system_of_a_credential() => _sessions.SystemOf(_other).ShouldEqual("Mail");
    [Fact] void should_not_find_a_system_for_a_credential_that_was_not_issued() => _sessions.SystemOf("nope").ShouldBeNull();
    [Fact] void should_issue_credentials_a_guess_cannot_find() => _first.Length.ShouldBeGreaterThanOrEqual(64);
}
