// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_preparing_a_root_beneath_a_usable_base : Specification
{
    string _directory = null!;
    string _result = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-prepare-spec-").FullName;

    void Because() => _result = SpecTemporaryRoot.PrepareRoot(_directory);

    [Fact] void should_use_a_root_beneath_the_base() => _result.ShouldEqual(Path.Combine(_directory, "cratis", "stage-spec-roots"));
    [Fact] void should_create_the_root() => Directory.Exists(_result).ShouldBeTrue();

    void Destroy() => SpecTemporaryRoot.Delete(_directory);
}
