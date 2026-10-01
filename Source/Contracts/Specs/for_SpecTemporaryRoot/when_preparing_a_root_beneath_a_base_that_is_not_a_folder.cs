// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_preparing_a_root_beneath_a_base_that_is_not_a_folder : Specification
{
    string _directory = null!;
    string _result = null!;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory("stage-prepare-spec-").FullName;
        File.WriteAllText(Path.Combine(_directory, "base"), "not a folder");
    }

    void Because() => _result = SpecTemporaryRoot.PrepareRoot(Path.Combine(_directory, "base"));

    [Fact] void should_fall_back_to_an_existing_temporary_folder() => Directory.Exists(_result).ShouldBeTrue();
    [Fact] void should_not_use_the_unusable_base() => _result.StartsWith(Path.Combine(_directory, "base")).ShouldBeFalse();

    void Destroy()
    {
        SpecTemporaryRoot.Delete(_result);
        SpecTemporaryRoot.Delete(_directory);
    }
}
