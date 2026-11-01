// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerProcess;

public class when_configuring_the_child_environment : Specification
{
    [Fact]
    void should_keep_runtime_roots_without_forwarding_unrelated_parent_values()
    {
        var parent = new Dictionary<string, string>
        {
            ["DOTNET_ROOT"] = "/fixture/dotnet",
            ["DOTNET_ROOT_ARM64"] = "/fixture/arm64",
            ["SystemRoot"] = "/fixture/system",
            ["PATH"] = "/fixture/private-bin",
            ["HOME"] = "/fixture/private-home",
            ["UPSTREAM_CREDENTIAL"] = "fixture"
        };
        var info = new ProcessStartInfo();
        BodyWorkerProcess.ConfigureEnvironment(info, "/fixture/cache", parent.GetValueOrDefault);
        info.Environment["DOTNET_ROOT"].ShouldEqual(parent["DOTNET_ROOT"]);
        info.Environment["DOTNET_ROOT_ARM64"].ShouldEqual(parent["DOTNET_ROOT_ARM64"]);
        info.Environment["SystemRoot"].ShouldEqual(parent["SystemRoot"]);
        info.Environment.ContainsKey("UPSTREAM_CREDENTIAL").ShouldBeFalse();
        info.Environment.ContainsKey("PATH").ShouldBeFalse();
        foreach (var name in new[] { "HOME", "TMPDIR", "TEMP", "TMP" }) info.Environment[name].ShouldEqual("/fixture/cache");
        info.Environment.Count.ShouldEqual(7);
    }

    [Fact]
    void should_not_require_optional_parent_runtime_configuration()
    {
        var info = new ProcessStartInfo();
        BodyWorkerProcess.ConfigureEnvironment(info, "/fixture/cache", _ => null);
        info.Environment.ContainsKey("DOTNET_ROOT").ShouldBeFalse();
        info.Environment.Count.ShouldEqual(4);
    }
}
