// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specs.for_SpecTemporaryRoot;

public class when_sweeping_a_root_that_others_can_write_to : Specification
{
    string _root = null!;
    string _stale = null!;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("stage-sweep-shared-spec-").FullName;
        _stale = Path.Combine(_root, "stale");
        Directory.CreateDirectory(_stale);
        Directory.SetLastWriteTimeUtc(_stale, DateTime.UtcNow.AddDays(-2));

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.OtherWrite);
        }
    }

    void Because() => SpecTemporaryRoot.Sweep(_root, DateTime.UtcNow.AddDays(-1));

    [Fact] void should_leave_the_stale_entry_where_the_mode_is_checked() => Directory.Exists(_stale).ShouldEqual(!OperatingSystem.IsWindows());

    void Destroy() => SpecTemporaryRoot.Delete(_root);
}
