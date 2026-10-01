// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_preparing_a_root_where_a_dangling_link_has_the_root_name : Specification
{
    string _directory = null!;
    string _result = null!;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory("stage-prepare-spec-").FullName;
        var parent = Path.Combine(_directory, "cratis");
        Directory.CreateDirectory(parent);
        Directory.CreateSymbolicLink(Path.Combine(parent, "stage-spec-roots"), Path.Combine(_directory, "missing"));
    }

    void Because() => _result = SpecTemporaryRoot.PrepareRoot(_directory);

    [Fact] void should_fall_back_to_an_existing_temporary_folder() => Directory.Exists(_result).ShouldBeTrue();
    [Fact] void should_not_use_the_base() => _result.StartsWith(_directory).ShouldBeFalse();

    void Destroy()
    {
        SpecTemporaryRoot.Delete(_result);
        SpecTemporaryRoot.Delete(_directory);
    }
}
