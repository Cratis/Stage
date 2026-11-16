// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_assets_cannot_be_read : given.an_asset_directory
{
    string _file = null!;
    string _folder = null!;
    bool _unreadable;

    void Establish()
    {
        Write("visible.svg", [1, 2]);
        Write("locked.svg", [3, 4]);
        Write("closed/inner.svg", [5, 6]);
        _file = Path.Combine(_assets, "locked.svg");
        _folder = Path.Combine(_assets, "closed");
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(_file, UnixFileMode.None);
        File.SetUnixFileMode(_folder, UnixFileMode.None);
        _unreadable = !CanOpen(_file);
    }

    void Because()
    {
        _result = ModelAssets.Read(_root);
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(_folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.SetUnixFileMode(_file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact] void should_keep_the_readable_asset() => _result.Files.ContainsKey("visible.svg").ShouldBeTrue();

    [Fact]
    void should_omit_unreadable_entries_with_a_warning()
    {
        if (!_unreadable) return;
        _result.Files.Keys.ShouldEqual(["visible.svg"]);
        _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(["STAGE-ASSET-003", "STAGE-ASSET-003"]);
    }

    static bool CanOpen(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
#endif
