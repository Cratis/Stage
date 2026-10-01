// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_sweeping_a_root_that_is_a_link : Specification
{
    string _directory = null!;
    string _stale = null!;

    void Establish()
    {
        _directory = Directory.CreateTempSubdirectory("stage-sweep-link-spec-").FullName;

        var target = Path.Combine(_directory, "target");
        Directory.CreateDirectory(target);
        _stale = Path.Combine(target, "stale");
        Directory.CreateDirectory(_stale);
        Directory.SetLastWriteTimeUtc(_stale, DateTime.UtcNow.AddDays(-2));

        Directory.CreateSymbolicLink(Path.Combine(_directory, "root"), target);
    }

    void Because() => SpecTemporaryRoot.Sweep(Path.Combine(_directory, "root"), DateTime.UtcNow.AddDays(-1));

    [Fact] void should_not_remove_anything_in_the_folder_the_link_points_to() => Directory.Exists(_stale).ShouldBeTrue();

    void Destroy() => SpecTemporaryRoot.Delete(_directory);
}
