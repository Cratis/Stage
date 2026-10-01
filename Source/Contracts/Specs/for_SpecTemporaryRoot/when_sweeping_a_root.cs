// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_sweeping_a_root : Specification
{
    string _root = null!;
    string _stale = null!;
    string _staleFile = null!;
    string _fresh = null!;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("stage-sweep-spec-").FullName;

        _stale = Path.Combine(_root, "stale");
        Directory.CreateDirectory(Path.Combine(_stale, "nested"));
        File.WriteAllText(Path.Combine(_stale, "nested", "file.txt"), "content");
        Directory.SetLastWriteTimeUtc(_stale, DateTime.UtcNow.AddDays(-2));

        _staleFile = Path.Combine(_root, "stale.txt");
        File.WriteAllText(_staleFile, "content");
        File.SetLastWriteTimeUtc(_staleFile, DateTime.UtcNow.AddDays(-2));

        _fresh = Path.Combine(_root, "fresh");
        Directory.CreateDirectory(_fresh);
    }

    void Because() => SpecTemporaryRoot.Sweep(_root, DateTime.UtcNow.AddDays(-1));

    [Fact] void should_remove_the_entry_that_is_two_days_old() => Directory.Exists(_stale).ShouldBeFalse();
    [Fact] void should_remove_the_file_that_is_two_days_old() => File.Exists(_staleFile).ShouldBeFalse();
    [Fact] void should_keep_the_fresh_entry() => Directory.Exists(_fresh).ShouldBeTrue();

    void Destroy() => SpecTemporaryRoot.Delete(_root);
}
