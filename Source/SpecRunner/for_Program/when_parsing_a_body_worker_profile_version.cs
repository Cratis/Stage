// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.SpecRunner.for_Program;

public class when_parsing_a_body_worker_profile_version : Specification
{
    [Fact] void should_read_the_forwarded_profile_version() => SpecRunnerArguments.Parse(["--model", "m", "--output", "o", "--body-worker-profile-version", "4"]).ProfileVersion.ShouldEqual("4");
    [Fact] void should_default_to_no_profile_version() => SpecRunnerArguments.Parse(["--model", "m", "--output", "o"]).ProfileVersion.ShouldBeNull();
}
