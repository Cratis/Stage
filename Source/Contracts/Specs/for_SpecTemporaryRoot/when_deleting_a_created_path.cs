// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_deleting_a_created_path : Specification
{
    string _path = null!;
    string _other = null!;

    void Establish()
    {
        _path = SpecTemporaryRoot.NewPath("stage-spec");
        _other = SpecTemporaryRoot.NewPath("stage-spec");
        Directory.CreateDirectory(Path.Combine(_path, "nested"));
        File.WriteAllText(Path.Combine(_path, "nested", "file.txt"), "content");
    }

    void Because()
    {
        SpecTemporaryRoot.Delete(_path);
        SpecTemporaryRoot.Delete(_other);
    }

    [Fact] void should_remove_the_path_and_everything_beneath_it() => Directory.Exists(_path).ShouldBeFalse();
    [Fact] void should_hand_out_distinct_paths() => _path.ShouldNotEqual(_other);
    [Fact] void should_place_the_path_beneath_a_spec_owned_root() => Path.GetFileName(Path.GetDirectoryName(_path)).ShouldEqual("stage-spec-roots");
}
