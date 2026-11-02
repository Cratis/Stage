// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_adding_a_binlog
{
    [Fact] void should_add_a_build_binlog() => Joined(["build"], "1").ShouldEqual($"build -bl:{Path.Combine("evidence", "build.binlog")}");
    [Fact] void should_add_a_restore_binlog() => Joined(["restore"], "1").ShouldEqual($"restore -bl:{Path.Combine("evidence", "build.binlog")}");
    [Fact] void should_leave_tests_unchanged() => Joined(["test"], "1").ShouldEqual("test");
    [Fact] void should_leave_unconfigured_arguments_unchanged() => Joined(["build"], null).ShouldEqual("build");
    [Fact] void should_leave_blank_configuration_unchanged() => Joined(["build"], " ").ShouldEqual("build");
    [Fact] void should_leave_disabled_arguments_unchanged() => Joined(["build"], "0").ShouldEqual("build");
    [Fact] void should_accept_true_case_insensitively() => Joined(["build"], "TrUe").ShouldEqual($"build -bl:{Path.Combine("evidence", "build.binlog")}");
    [Fact] void should_keep_an_existing_dash_binlog() => Joined(["build", "-bl:existing.binlog"], "1").ShouldEqual("build -bl:existing.binlog");
    [Fact] void should_keep_an_existing_slash_binlog() => Joined(["restore", "/bl"], "1").ShouldEqual("restore /bl");
    [Fact] void should_leave_empty_arguments_unchanged() => Joined([], "1").ShouldEqual(string.Empty);

    static string Joined(string[] arguments, string? enabled) => string.Join(' ', GeneratedApplicationBuilds.WithBinlog(arguments, enabled, "evidence", "build.log"));
}
