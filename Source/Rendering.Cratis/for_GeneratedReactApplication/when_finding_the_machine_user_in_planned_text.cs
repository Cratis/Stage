// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication;

/// <summary>
/// The machine-identity check the determinism specification relies on, against planted text: it must still find a
/// user that leaked, and must not mistake the user name for an ordinary word.
/// </summary>
public class when_finding_the_machine_user_in_planned_text : Specification
{
    static readonly string _user = Environment.UserName;

    [Fact] void should_find_a_planted_unix_home_path() => MachineIdentity.ContainsUser($"root = '/home/{_user}/work/app';", _user).ShouldBeTrue();
    [Fact] void should_find_a_planted_macos_home_path() => MachineIdentity.ContainsUser($"/Users/{_user}/src", _user).ShouldBeTrue();
    [Fact] void should_find_a_planted_windows_profile_path() => MachineIdentity.ContainsUser($"C:\\Users\\{_user}\\src", _user).ShouldBeTrue();
    [Fact] void should_find_a_path_ending_in_the_user() => MachineIdentity.ContainsUser($"HOME=/home/{_user}", _user).ShouldBeTrue();
    [Fact] void should_find_a_planted_user_at_host() => MachineIdentity.ContainsUser($"author: {_user}@{Environment.MachineName}", _user).ShouldBeTrue();
    [Fact] void should_report_a_planted_home_path_through_the_full_check() => MachineIdentity.FoundIn($"/home/{_user}/").ShouldNotBeEmpty();
    [Fact] void should_report_a_planted_user_at_host_through_the_full_check() => MachineIdentity.FoundIn($"{_user}@ci-host").ShouldNotBeEmpty();
    [Fact] void should_find_the_ci_runner_user_in_its_home_path() => MachineIdentity.ContainsUser("/home/runner/work/Stage", "runner").ShouldBeTrue();
    [Fact] void should_find_the_ci_runner_user_at_its_host() => MachineIdentity.ContainsUser("runner@fv-az123", "runner").ShouldBeTrue();
    [Fact] void should_not_mistake_a_package_id_for_the_ci_runner_user() => MachineIdentity.ContainsUser("<PackageReference Include=\"xunit.runner.visualstudio\" />", "runner").ShouldBeFalse();
    [Fact] void should_not_mistake_a_longer_path_segment_for_the_user() => MachineIdentity.ContainsUser("/opt/runners/bin", "runner").ShouldBeFalse();
    [Fact] void should_not_mistake_a_longer_identity_for_the_user() => MachineIdentity.ContainsUser("test-runner@host", "runner").ShouldBeFalse();
}
