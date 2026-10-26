// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_capping_the_build_nodes
{
    [Fact] void should_cap_a_dotnet_build() => Joined(GeneratedApplicationBuilds.Capped("dotnet", ["build", "App.slnx", "-c", "Debug"])).ShouldEqual(Joined(["build", "-m:3", "App.slnx", "-c", "Debug"]));
    [Fact] void should_cap_a_dotnet_test_before_the_runsettings() => Joined(GeneratedApplicationBuilds.Capped("dotnet", ["test", "App.csproj", "--", "x=1"])).ShouldEqual(Joined(["test", "-m:3", "App.csproj", "--", "x=1"]));
    [Fact] void should_keep_an_existing_cap() => Joined(GeneratedApplicationBuilds.Capped("dotnet", ["build", "-m:2"])).ShouldEqual(Joined(["build", "-m:2"]));
    [Fact] void should_leave_other_dotnet_verbs() => Joined(GeneratedApplicationBuilds.Capped("dotnet", ["run", "--no-build"])).ShouldEqual(Joined(["run", "--no-build"]));
    [Fact] void should_leave_other_tools() => Joined(GeneratedApplicationBuilds.Capped("npm", ["run", "build"])).ShouldEqual(Joined(["run", "build"]));

    static string Joined(string[] arguments) => string.Join(' ', arguments);
}
