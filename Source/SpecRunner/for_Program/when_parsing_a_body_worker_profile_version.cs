// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

public class when_parsing_a_body_worker_profile_version : Specification
{
    [Fact] void should_read_the_forwarded_profile_version() => Parse("4").ProfileVersion.ShouldEqual("4");
    [Fact] void should_read_the_retained_profile_version() => Parse("3").ProfileVersion.ShouldEqual("3");
    [Fact] void should_default_to_no_profile_version() => SpecRunnerArguments.Parse(["--model", "m", "--output", "o"]).ProfileVersion.ShouldBeNull();

    [Theory]
    [InlineData("99")]
    [InlineData("5")]
    [InlineData("v4")]
    void should_reject_an_unsupported_profile_version_naming_it_and_the_supported_versions(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => Parse(value));
        Assert.Contains($"'{value}'", exception.Message);
        Assert.Contains("Supported versions are 3 and 4", exception.Message);
    }

    static SpecRunnerArguments Parse(string version) => SpecRunnerArguments.Parse(["--model", "m", "--output", "o", "--body-worker-profile-version", version]);
}
