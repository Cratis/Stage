// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationBuilds;

public class when_formatting_a_timing
{
    static readonly DateTimeOffset _started = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact] void should_format_the_started_time_caller_verb_and_seconds() => Line("dotnet", ["build", "-c", "Debug"]).ShouldEqual("2026-11-01T12:00:00.0000000+00:00\tCaller\tdotnet build\tDebug\t1.2\t3.4");
    [Fact] void should_read_the_long_configuration_option() => Line("dotnet", ["build", "--configuration", "Release"]).Split('\t')[3].ShouldEqual("Release");
    [Fact] void should_read_the_configuration_property() => Line("dotnet", ["build", "-p:Configuration=Release"]).Split('\t')[3].ShouldEqual("Release");
    [Fact] void should_leave_missing_configuration_unspecified() => Line("dotnet", ["test"]).Split('\t')[3].ShouldEqual("-");
    [Fact] void should_format_an_npm_process() => Line("npm", ["run", "build"]).ShouldEqual("2026-11-01T12:00:00.0000000+00:00\tCaller\tnpm run\t-\t1.2\t3.4");
    [Fact] void should_leave_an_incomplete_configuration_unspecified() => Line("dotnet", ["build", "-c"]).Split('\t')[3].ShouldEqual("-");
    [Fact]
    void should_use_invariant_seconds()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Line("dotnet", ["build"]).Split('\t')[4].ShouldEqual("1.2");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    static string Line(string fileName, string[] arguments) => GeneratedApplicationBuilds.TimingLine(_started, new("Caller", fileName, arguments), TimeSpan.FromSeconds(1.2), TimeSpan.FromSeconds(3.4));
}
