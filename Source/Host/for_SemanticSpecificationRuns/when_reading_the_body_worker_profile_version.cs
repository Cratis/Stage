// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_reading_the_body_worker_profile_version : Specification
{
    [Fact]
    void should_default_to_no_profile_version() => SemanticBodyWorkerOptions.From(new ConfigurationBuilder().Build()).ProfileVersion.ShouldBeNull();

    [Theory]
    [InlineData("3", "3")]
    [InlineData("4", "4")]
    [InlineData(" 4 ", "4")]
    [InlineData("", null)]
    [InlineData("  ", null)]
    void should_read_the_configured_profile_version(string value, string? expected) => Read(value).ProfileVersion.ShouldEqual(expected);

    [Theory]
    [InlineData("99")]
    [InlineData("5")]
    [InlineData("v4")]
    void should_reject_an_unsupported_profile_version_naming_it_and_the_supported_versions(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Read(value));
        Assert.Contains($"'{value}'", exception.Message);
        Assert.Contains("Supported versions are 3 and 4", exception.Message);
    }

    static SemanticBodyWorkerOptions Read(string value) =>
        SemanticBodyWorkerOptions.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Stage:BodyWorker:ProfileVersion"] = value }).Build());
}
