// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxUsers;

public class when_reading_the_users_switch : Specification
{
    static IConfiguration With(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value))).Build();

    SandboxUsersOptions _unset = null!;
    SandboxUsersOptions _on = null!;
    SandboxUsersOptions _remote = null!;
    SandboxUsersOptions _remoteOff = null!;
    Exception? _refused;

    void Because()
    {
        _unset = SandboxUsersOptions.Read(With(), development: true);
        _on = SandboxUsersOptions.Read(With((SandboxUsersOptions.Key, "true")), development: true);
        _remote = SandboxUsersOptions.Read(With((SandboxUsersOptions.Key, "true"), ($"{SandboxUsersOptions.Key}:AllowRemote", "true")), development: true);
        _remoteOff = SandboxUsersOptions.Read(With((SandboxUsersOptions.Key, "true"), ($"{SandboxUsersOptions.Key}:AllowRemote", "false")), development: true);
        _refused = Catch.Exception(() => SandboxUsersOptions.Read(With((SandboxUsersOptions.Key, "true")), development: false));
    }

    [Fact] void should_be_off_unless_it_is_set() => _unset.Enabled.ShouldBeFalse();
    [Fact] void should_be_on_when_set_in_development() => _on.Enabled.ShouldBeTrue();
    [Fact] void should_only_accept_local_users_by_default() => _on.AllowRemote.ShouldBeFalse();
    [Fact] void should_accept_remote_users_when_allowed() => _remote.AllowRemote.ShouldBeTrue();
    [Fact] void should_leave_remote_users_off_when_disabled() => _remoteOff.AllowRemote.ShouldBeFalse();
    [Fact] void should_refuse_to_start_when_set_outside_development() => _refused.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_say_why_it_refused() => _refused!.Message.ShouldContain("development");
}
