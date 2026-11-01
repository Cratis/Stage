// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyWorkerLaunch;

public class when_launching_the_current_process : Specification
{
    [Theory]
    [InlineData("/tools/cratis", "")]
    [InlineData("/tools/cratis", "/tools/cratis.dll")]
    [InlineData("/tools/dotnet", "/tools/cratis.dll")]
    [InlineData("C:/tools/dotnet.exe", "C:/tools/cratis.dll")]
    void should_only_prepend_the_entry_assembly_for_the_dotnet_host(string process, string entry)
    {
        var launch = BodyWorkerLaunch.ForCurrentProcess("__worker", process, entry);
        launch.FileName.ShouldEqual(process);
        launch.Arguments.ToArray().ShouldEqual(process.EndsWith("dotnet", StringComparison.Ordinal) || process.EndsWith("dotnet.exe", StringComparison.Ordinal) ? [entry, "__worker"] : ["__worker"]);
    }

    [Fact]
    void should_refuse_a_missing_process_path() => Assert.Throws<InvalidOperationException>(() => BodyWorkerLaunch.ForCurrentProcess("__worker", null, ""));
}
