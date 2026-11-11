// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

public class when_refusing_an_unsupported_body_worker_profile_version : Specification
{
    int _exitCode;
    string _error = string.Empty;

    async Task Because()
    {
        await using var error = new StringWriter();
        _exitCode = await Program.Run(["--engine", "semantic", "--model", "model.play", "--output", "results.json", "--body-worker-profile-version", "99"], TextWriter.Null, error);
        _error = error.ToString();
    }

    [Fact] void should_exit_with_an_argument_error() => _exitCode.ShouldEqual(2);
    [Fact] void should_name_the_unsupported_version() => Assert.Contains("'99'", _error);
    [Fact] void should_name_the_supported_versions() => Assert.Contains("Supported versions are 3 and 4", _error);
}
